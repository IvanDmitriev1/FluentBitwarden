using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal sealed class IpcTestHost : IAsyncDisposable
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly IHost _host;
    private int _disposed;

    private IpcTestHost(IHost host, string pipeName, TestIpcClientsVerifier verifier)
    {
        _host = host;
        PipeName = pipeName;
        Verifier = verifier;
    }

    public string PipeName { get; }

    public TestIpcClientsVerifier Verifier { get; }

    public IIpcClient Client => _host.Services.GetRequiredService<IIpcClient>();

    public THandler Handler<THandler>()
        where THandler : class, IIpcRequestsHandler =>
        _host.Services.GetRequiredService<THandler>();

    public Task StopAsync(CancellationToken cancellationToken) => _host.StopAsync(cancellationToken);

    public static async Task<IpcTestHost> StartAsync<THandler>(
        IpcAuthenticationLevel authenticationLevel = IpcAuthenticationLevel.SamePackage)
        where THandler : class, IIpcRequestsHandler
    {
        string pipeName = $"FluentBitwarden.Tests.{Guid.NewGuid():N}";
        var verifier = new TestIpcClientsVerifier(authenticationLevel);
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
        });

        builder.Services.AddLogging();
        builder.Services.AddSingleton<IIpcClientsVerifier>(verifier);

#pragma warning disable IL2026, IL2091, IL3050 // The test intentionally exercises the reflection-based registration boundary.
builder.Services.AddIpcServer(pipeName, handlers => handlers.Add<THandler>());
#pragma warning restore IL2026, IL2091, IL3050
        builder.Services.AddIpcClient(pipeName);

        IHost host = builder.Build();

        try
        {
            await host.StartAsync();
            return new IpcTestHost(host, pipeName, verifier);
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            await _host.StopAsync();
        }
        finally
        {
            _host.Dispose();
        }
    }
}
