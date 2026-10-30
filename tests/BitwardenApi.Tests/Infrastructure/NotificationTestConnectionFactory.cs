using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System.IO.Pipelines;

namespace BitwardenApi.Tests.Infrastructure;

internal sealed class NotificationTestConnectionFactory(params Exception[] connectFailures)
    : IConnectionFactory, IAsyncDisposable
{
    private readonly Queue<Exception> _connectFailures = new(connectFailures);
    private readonly List<NotificationTestConnection> _connections = [];
    private readonly List<EndPoint> _endPoints = [];
    private readonly TaskCompletionSource<NotificationTestConnection> _connectionCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _connectStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _allowConnect = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int ConnectCount { get; private set; }

    public bool InvokeAccessTokenProvider { get; set; }

    public bool BlockConnect { get; set; }

    public Task ConnectStarted => _connectStarted.Task;

    public void AllowConnect() => _allowConnect.TrySetResult(true);

    public string? AccessToken { get; private set; }

    public HttpTransportType Transports { get; private set; }

    public bool SkipNegotiation { get; private set; }

    public IReadOnlyList<EndPoint> EndPoints => _endPoints;

    public IReadOnlyList<NotificationTestConnection> Connections => _connections;

    public Task<NotificationTestConnection> ConnectionCreated => _connectionCreated.Task;

    private Func<Task<string?>>? AccessTokenProvider { get; set; }

    public void Configure(IHubConnectionBuilder builder)
    {
        builder.Services.AddSingleton<IConnectionFactory>(services =>
        {
            HttpConnectionOptions options = services.GetRequiredService<IOptions<HttpConnectionOptions>>().Value;
            AccessTokenProvider = options.AccessTokenProvider;
            Transports = options.Transports;
            SkipNegotiation = options.SkipNegotiation;
            return this;
        });
    }

    public async ValueTask<ConnectionContext> ConnectAsync(
        EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        ConnectCount++;
        _endPoints.Add(endPoint);
        _connectStarted.TrySetResult(true);
        if (BlockConnect)
        {
            await _allowConnect.Task.WaitAsync(cancellationToken);
        }

        if (_connectFailures.TryDequeue(out Exception? failure))
        {
            throw failure;
        }

        if (InvokeAccessTokenProvider)
        {
            AccessToken = await (AccessTokenProvider
                ?? throw new InvalidOperationException("The HTTP access-token callback was not configured."))();
        }

        var connection = new NotificationTestConnection($"notification-{ConnectCount}");
        _connections.Add(connection);
        await connection.SendHandshakeResponseAsync(cancellationToken);
        _connectionCreated.TrySetResult(connection);
        return connection.Context;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (NotificationTestConnection connection in _connections)
        {
            await connection.DisposeAsync();
        }
    }

    internal sealed class NotificationTestConnection : IAsyncDisposable
    {
        private readonly Pipe _clientToServer = new();
        private readonly Pipe _serverToClient = new();

        public NotificationTestConnection(string connectionId)
        {
            var transport = new TestDuplexPipe(_serverToClient.Reader, _clientToServer.Writer);
            var application = new TestDuplexPipe(_clientToServer.Reader, _serverToClient.Writer);
            Context = new NotificationTestConnectionContext(
                connectionId,
                transport,
                application,
                CompleteApplicationPipes);
        }

        public NotificationTestConnectionContext Context { get; }

        public Task Disposed => Context.Disposed;

        public async Task SendHubMessageAsync(string json, CancellationToken cancellationToken = default)
        {
            byte[] frame = System.Text.Encoding.UTF8.GetBytes(json + "\u001e");
            await _serverToClient.Writer.WriteAsync(frame, cancellationToken);
        }

        public async Task SendHandshakeResponseAsync(CancellationToken cancellationToken)
        {
            await _serverToClient.Writer.WriteAsync("{}\u001e"u8.ToArray(), cancellationToken);
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();

        private void CompleteApplicationPipes()
        {
            _clientToServer.Reader.Complete();
            _serverToClient.Writer.Complete();
        }

        private sealed class TestDuplexPipe(PipeReader input, PipeWriter output) : IDuplexPipe
        {
            public PipeReader Input { get; } = input;

            public PipeWriter Output { get; } = output;
        }
    }

    internal sealed class NotificationTestConnectionContext : DefaultConnectionContext
    {
        private readonly CancellationTokenSource _connectionClosed = new();
        private readonly Action _completeApplicationPipes;
        private readonly TaskCompletionSource<bool> _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _applicationPipesCompleted;
        private int _disposeStarted;

        public NotificationTestConnectionContext(
            string connectionId,
            IDuplexPipe transport,
            IDuplexPipe application,
            Action completeApplicationPipes)
            : base(connectionId, transport, application)
        {
            _completeApplicationPipes = completeApplicationPipes;
            ConnectionClosed = _connectionClosed.Token;
        }

        public Task Disposed => _disposed.Task;

        // DefaultConnectionContext queues cancellation then disposes its source; pipe closure avoids that race.
        public override void Abort() => CompleteApplicationPipes();

        public override void Abort(ConnectionAbortedException abortReason) => CompleteApplicationPipes();

        public override ValueTask DisposeAsync() => Interlocked.Exchange(ref _disposeStarted, 1) == 0
            ? DisposeFirstAsync()
            : new ValueTask(_disposed.Task);

        private async ValueTask DisposeFirstAsync()
        {
            try
            {
                _connectionClosed.Cancel();
                CompleteApplicationPipes();
                await base.DisposeAsync();
            }
            finally
            {
                _connectionClosed.Dispose();
                _disposed.TrySetResult(true);
            }
        }

        private void CompleteApplicationPipes()
        {
            if (Interlocked.Exchange(ref _applicationPipesCompleted, 1) == 0)
            {
                _completeApplicationPipes();
            }
        }
    }
}
