using System.Runtime.InteropServices;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Platform.Ipc.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.CommandPalette;

[Guid("284C1158-2735-42BB-A49F-269B620CB905")]
public sealed partial class FluentBitwardenCommandPaletteExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposed;
    private readonly ServiceProvider _services;
    private readonly FluentBitwardenCommandsProvider _provider;
    private int _disposed;

    public FluentBitwardenCommandPaletteExtension(ManualResetEvent extensionDisposed)
    {
        _extensionDisposed = extensionDisposed;

        var services = new ServiceCollection();
        services.AddIpcClient(IpcConstants.AppHostPipeName);
        _services = services.BuildServiceProvider();

        FluentBitwardenProcessLauncher.EnsureAppHostRunning();

        var client = new AppHostClient(_services.GetRequiredService<IIpcClient>());
        _provider = new FluentBitwardenCommandsProvider(client);
    }

    public object? GetProvider(ProviderType providerType) =>
        providerType == ProviderType.Commands ? _provider : null;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            _services.Dispose();
        }
        finally
        {
            _extensionDisposed.Set();
        }
    }
}
