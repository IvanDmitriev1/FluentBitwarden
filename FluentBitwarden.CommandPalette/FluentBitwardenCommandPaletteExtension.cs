using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;

namespace FluentBitwarden.CommandPalette;

[Guid("284C1158-2735-42BB-A49F-269B620CB905")]
public sealed partial class FluentBitwardenCommandPaletteExtension : IExtension, IDisposable
{
    private readonly FluentBitwardenCommandsProvider _provider;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private int _disposed;

    internal FluentBitwardenCommandPaletteExtension(
        FluentBitwardenCommandsProvider provider,
        IHostApplicationLifetime applicationLifetime)
    {
        _provider = provider;
        _applicationLifetime = applicationLifetime;
    }

    public object? GetProvider(ProviderType providerType) =>
        providerType == ProviderType.Commands ? _provider : null;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _applicationLifetime.StopApplication();
    }
}
