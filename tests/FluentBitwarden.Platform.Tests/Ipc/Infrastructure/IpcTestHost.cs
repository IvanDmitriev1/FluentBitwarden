using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal sealed class IpcTestHost : IAsyncDisposable
{
    public const int TimeoutMilliseconds = 10_000;
    public static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(TimeoutMilliseconds);

    private readonly IHost _host;
    private readonly CancellationToken _cancellationToken;
    private int _disposed;

    internal IpcTestHost(
        IHost host,
        string pipeName,
        TestIpcClientsVerifier verifier,
        CancellationToken cancellationToken)
    {
        _host = host;
        _cancellationToken = cancellationToken;
        PipeName = pipeName;
        Verifier = verifier;
    }

    public string PipeName { get; }

    public TestIpcClientsVerifier Verifier { get; }

    public IIpcClient Client => _host.Services.GetRequiredService<IIpcClient>();

    public Task StopAsync(CancellationToken cancellationToken) => _host.StopAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            await _host.StopAsync(_cancellationToken);
        }
        finally
        {
            _host.Dispose();
        }
    }
}
