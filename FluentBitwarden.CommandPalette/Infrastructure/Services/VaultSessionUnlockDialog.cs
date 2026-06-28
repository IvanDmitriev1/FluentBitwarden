using FluentBitwarden.Contracts.Infrastructure;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.CommandPalette.Infrastructure.Services;

internal sealed class VaultSessionUnlockDialog(IIpcEventClient eventClient, IUiProcessManager uiProcessManager) : IVaultSessionUnlockDialog
{
    public async Task WaitUntilUnlockAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        uiProcessManager.Activate();
        uiProcessManager.ProcessExited += ProcessOnExited;

        try
        {
            var waitTask = eventClient.WaitAsync<VaultSessionStatusChangedEvent>(cts.Token);
            await waitTask;
        }
        finally
        {
            uiProcessManager.ProcessExited -= ProcessOnExited;
            uiProcessManager.Exit();
        }

        return;
        void ProcessOnExited()
        {
            cts.Cancel();
        }
    }
}
