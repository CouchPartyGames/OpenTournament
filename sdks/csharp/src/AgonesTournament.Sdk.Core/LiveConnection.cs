using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace AgonesTournament.Sdk.Core;

/// <summary>One WebSocket for any number of Tournaments. Consume NotificationsAsync once; disposal closes the connection.</summary>
public sealed class LiveConnection : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Uri endpoint;
    private readonly Func<CancellationToken, Task<string>>? tokenProvider;
    private readonly Channel<(TournamentId Id, bool Subscribe)> commands = Channel.CreateUnbounded<(TournamentId, bool)>();
    private readonly Channel<LiveNotification> notifications = Channel.CreateBounded<LiveNotification>(256);
    private readonly CancellationTokenSource lifetime = new();
    private Task? runner;
    private int reader;

    /// <summary>Creates a connection using an HTTP(S) service root, preserving deployment prefixes.</summary>
    public LiveConnection(Uri serviceRoot, Func<CancellationToken, Task<string>>? accessTokenProvider = null)
    {
        ArgumentNullException.ThrowIfNull(serviceRoot);
        if (!serviceRoot.IsAbsoluteUri || serviceRoot.Scheme is not ("http" or "https")
            || serviceRoot.Query.Length != 0 || serviceRoot.Fragment.Length != 0)
            throw new ArgumentException("Supply an absolute HTTP(S) service root without a query or fragment.", nameof(serviceRoot));
        var uri = new Uri(serviceRoot.AbsoluteUri.TrimEnd('/') + "/api/v1/live");
        endpoint = new UriBuilder(uri) { Scheme = uri.Scheme == "https" ? "wss" : "ws" }.Uri;
        tokenProvider = accessTokenProvider;
    }

    /// <summary>Queues a subscription. LiveResync announces the acknowledged baseline; then fetch state over REST.</summary>
    public void Subscribe(TournamentId tournamentId) => Queue(tournamentId, true);

    /// <summary>Queues removal of a subscription. Already queued notifications may still be read.</summary>
    public void Unsubscribe(TournamentId tournamentId) => Queue(tournamentId, false);

    private void Queue(TournamentId id, bool subscribe)
    {
        ObjectDisposedException.ThrowIf(lifetime.IsCancellationRequested, this);
        if (!commands.Writer.TryWrite((id, subscribe)))
            throw new InvalidOperationException("The live connection has ended.");
    }

    /// <summary>Reads ordered events and resync baselines. Reconnects with exponential backoff (250ms to 30s), refreshing authentication.</summary>
    public async IAsyncEnumerable<LiveNotification> NotificationsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref reader, 1) != 0)
            throw new InvalidOperationException("A live connection supports one notification reader.");
        runner = RunAsync(lifetime.Token);
        try
        {
            await foreach (var notification in notifications.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return notification;
        }
        finally
        {
            await lifetime.CancelAsync().ConfigureAwait(false);
            await runner.ConfigureAwait(false);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var subscriptions = new Dictionary<TournamentId, long?>();
        var delay = 250;
        Exception? failure = null;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var socket = new ClientWebSocket();
                    await socket.ConnectAsync(endpoint, ct).ConfigureAwait(false);
                    if (tokenProvider is not null)
                    {
                        var token = await tokenProvider(ct).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(token))
                            throw new InvalidOperationException("The Keycloak access token provider returned an empty token.");
                        await SendAsync(socket, new { type = "authenticate", token }, ct).ConfigureAwait(false);
                        var auth = await ReceiveAsync(socket, ct).ConfigureAwait(false);
                        CheckError(auth);
                        if (auth.GetProperty("type").GetString() != "authenticated")
                            throw new LiveProtocolException("bad-message", "Expected authentication acknowledgement.");
                    }
                    foreach (var id in subscriptions.Keys.ToArray())
                    {
                        subscriptions[id] = null;
                        await SendAsync(socket, new { type = "subscribe", tournamentId = id }, ct).ConfigureAwait(false);
                    }
                    using var session = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    var receive = ReceiveAsync(socket, session.Token);
                    var commandReady = commands.Reader.WaitToReadAsync(session.Token).AsTask();
                    try
                    {
                        while (true)
                        {
                            var ready = await Task.WhenAny(receive, commandReady).ConfigureAwait(false);
                            if (ready == commandReady)
                            {
                                await commandReady.ConfigureAwait(false);
                                while (commands.Reader.TryRead(out var command))
                                {
                                    if (command.Subscribe)
                                    {
                                        if (subscriptions.ContainsKey(command.Id)) continue;
                                        subscriptions[command.Id] = null;
                                    }
                                    else if (!subscriptions.Remove(command.Id)) continue;
                                    await SendAsync(socket, new { type = command.Subscribe ? "subscribe" : "unsubscribe", tournamentId = command.Id }, ct).ConfigureAwait(false);
                                }
                                commandReady = commands.Reader.WaitToReadAsync(session.Token).AsTask();
                                continue;
                            }
                            var message = await receive.ConfigureAwait(false);
                            CheckError(message);
                            var type = message.GetProperty("type").GetString();
                            if (type is "subscribed" or "event")
                            {
                                var id = message.GetProperty("tournamentId").Deserialize<TournamentId>(Json);
                                if (subscriptions.TryGetValue(id, out var last))
                                {
                                    var seq = message.TryGetProperty("seq", out var s) ? s.GetInt64() : 0;
                                    if (type == "subscribed")
                                    {
                                        subscriptions[id] = seq;
                                        await notifications.Writer.WriteAsync(new LiveResync(id, seq), ct).ConfigureAwait(false);
                                        delay = 250;
                                    }
                                    else if (last is not null && seq > last)
                                    {
                                        if (seq != last + 1) throw new WebSocketException("Live sequence gap; resubscribe and refetch.");
                                        subscriptions[id] = seq;
                                        await notifications.Writer.WriteAsync(ParseEvent(id, seq, message), ct).ConfigureAwait(false);
                                    }
                                }
                            }
                            receive = ReceiveAsync(socket, session.Token);
                        }
                    }
                    finally
                    {
                        await session.CancelAsync().ConfigureAwait(false);
                        socket.Abort();
                        try { await receive.ConfigureAwait(false); } catch (Exception) { /* Observe the pending receive on shutdown. */ }
                        try { await commandReady.ConfigureAwait(false); } catch (OperationCanceledException) { }
                    }
                }
                catch (Exception error) when (error is WebSocketException or HttpRequestException or IOException)
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                    delay = Math.Min(delay * 2, 30000);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception error) { failure = error; }
        finally
        {
            commands.Writer.TryComplete(failure);
            notifications.Writer.TryComplete(failure);
        }
    }

    private static void CheckError(JsonElement message)
    {
        if (message.GetProperty("type").GetString() == "error")
            throw new LiveProtocolException(message.GetProperty("code").GetString()!, message.GetProperty("error").GetString()!);
    }

    private static LiveEvent ParseEvent(TournamentId id, long seq, JsonElement message)
    {
        var type = message.GetProperty("event").GetString()!;
        var data = message.GetProperty("data");
        var payloadType = type switch
        {
            "tournament.status-changed" => typeof(TournamentStatusChanged),
            "tournament.completed" => typeof(TournamentCompleted),
            "registrations.changed" => typeof(RegistrationsChanged),
            "participant.changed" => typeof(ParticipantChanged),
            "stage.started" => typeof(StageStarted),
            "stage.completed" => typeof(StageCompleted),
            "match.changed" => typeof(MatchChanged),
            "bout.recorded" => typeof(BoutRecorded),
            "standings.changed" => typeof(StandingsChanged),
            _ => null
        };
        return new LiveEvent(id, seq, type, data, payloadType is null ? null : data.Deserialize(payloadType, Json));
    }

    private static async Task SendAsync(ClientWebSocket socket, object message, CancellationToken ct)
        => await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message, Json), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);

    private static async Task<JsonElement> ReceiveAsync(ClientWebSocket socket, CancellationToken ct)
    {
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        WebSocketReceiveResult part;
        do
        {
            part = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
            if (part.MessageType == WebSocketMessageType.Close) throw new WebSocketException("Live connection closed.");
            if (part.MessageType != WebSocketMessageType.Text) throw new LiveProtocolException("bad-message", "Expected a text message.");
            body.Write(buffer, 0, part.Count);
            if (body.Length > 4 * 1024 * 1024) throw new LiveProtocolException("bad-message", "Live message exceeds 4 MiB.");
        } while (!part.EndOfMessage);
        using var json = JsonDocument.Parse(body.ToArray());
        return json.RootElement.Clone();
    }

    /// <summary>Closes the socket and waits for pending work to stop.</summary>
    public async ValueTask DisposeAsync()
    {
        await lifetime.CancelAsync().ConfigureAwait(false);
        if (runner is not null) await runner.ConfigureAwait(false);
    }
}
