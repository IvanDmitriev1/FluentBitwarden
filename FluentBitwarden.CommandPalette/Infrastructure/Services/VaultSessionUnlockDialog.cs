using FluentBitwarden.Contracts.Infrastructure;

using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.CommandPalette.Infrastructure.Services;

internal sealed class VaultSessionUnlockDialog(IIpcEventClient eventClient) : IVaultSessionUnlockDialog
{
    public async Task WaitUntilUnlockAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var process = FluentBitwardenProcessLauncher.OpenUnlockOverlay();
        process.EnableRaisingEvents = true;
        process.Exited += ProcessOnExited;

        try
        {
            var waitTask = eventClient.WaitAsync<VaultSessionStatusChangedEvent>(cts.Token);
            await waitTask;
        }
        finally
        {
            process.Exited -= ProcessOnExited;

            process.CloseMainWindow();
            process.Dispose();
        }

        return;
        void ProcessOnExited(object? sender, EventArgs e)
        {
            cts.Cancel();
        }
    }
}
