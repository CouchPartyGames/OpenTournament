using System.Threading.Channels;
using Agones.Dev.Sdk;
using Grpc.Core;
using AgonesServer = Agones.Dev.Sdk.GameServer;

namespace AgonesTournament.Sdk.Tests;

// Exercises the real Agones C# SDK against an in-memory gRPC transport.
internal sealed class AgonesTransport(AgonesServer snapshot) : CallInvoker
{
    internal Channel<AgonesServer> Updates { get; } = Channel.CreateUnbounded<AgonesServer>();

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
    {
        Assert.Equal("/agones.dev.sdk.SDK/GetGameServer", method.FullName);
        return new(Task.FromResult((TResponse)(object)snapshot), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
    {
        Assert.Equal("/agones.dev.sdk.SDK/WatchGameServer", method.FullName);
        return new(new Reader<TResponse>(Updates.Reader), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method,
        string? host, CallOptions options, TRequest request) => throw new NotSupportedException();
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();

    private sealed class Reader<T>(ChannelReader<AgonesServer> updates) : IAsyncStreamReader<T>
    {
        public T Current { get; private set; } = default!;
        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            Current = (T)(object)await updates.ReadAsync(cancellationToken);
            return true;
        }
    }
}
