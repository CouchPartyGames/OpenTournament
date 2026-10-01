using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgonesTournament.Sdk.Tests;

internal sealed class WebSocketServer(WebApplication app, Uri serviceRoot) : IAsyncDisposable
{
    public Uri ServiceRoot { get; } = serviceRoot;

    public static async Task<WebSocketServer> StartAsync(Func<WebSocket, CancellationToken, Task> serve)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.UseWebSockets();
        app.Map("/prefix/api/v1/live", async context =>
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            try { await serve(socket, context.RequestAborted); }
            catch (Exception error) when (error is OperationCanceledException or WebSocketException) { }
        });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new WebSocketServer(app, new Uri(address + "/prefix"));
    }

    public static async Task<JsonElement> ReadAsync(WebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[8192];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        Assert.True(result.EndOfMessage);
        using var json = JsonDocument.Parse(buffer.AsMemory(0, result.Count));
        return json.RootElement.Clone();
    }

    public static async Task SendAsync(WebSocket socket, string message, CancellationToken ct)
        => await socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, ct);

    // Unlike Abort, a close is ordered after the frames already sent, so the client
    // always reads them before it sees the connection drop.
    public static async Task DropAsync(WebSocket socket, CancellationToken ct)
        => await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, ct);

    public static async Task SubscribeAsync(WebSocket socket, string id, long seq, CancellationToken ct)
    {
        var request = await ReadAsync(socket, ct);
        Assert.Equal("subscribe", request.GetProperty("type").GetString());
        Assert.Equal(id, request.GetProperty("tournamentId").GetString());
        await SendAsync(socket, $$"""{"type":"subscribed","tournamentId":"{{id}}","seq":{{seq}}}""", ct);
    }

    public static string Event(string id, long seq, string type, string data)
        => $$"""{"type":"event","tournamentId":"{{id}}","seq":{{seq}},"event":"{{type}}","data":{{data}}}""";

    public async ValueTask DisposeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await app.StopAsync(timeout.Token);
        await app.DisposeAsync();
    }
}
