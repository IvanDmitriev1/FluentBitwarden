using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.Contracts.Infrastructure;
using FluentBitwarden.Contracts.Modules.Vault;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSessionUnlockDialog(
    IVaultSessionCoordinator vaultSessionCoordinator,
    IUiProcessLauncher uiProcessLauncher) : IVaultSessionUnlockDialog
{
    public async ValueTask WaitUntilUnlockAsync(CancellationToken cancellationToken)
    {
        if (vaultSessionCoordinator.TryGetUnlockedSession(out var session))
            return;

        cancellationToken.ThrowIfCancellationRequested();

        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var _ = cancellationToken.Register(OnCancelled);
        vaultSessionCoordinator.SessionStatusChanged += VaultSessionCoordinatorOnSessionStatusChanged;

        try
        {
            uiProcessLauncher.Activate();
            await tcs.Task;
        }
        finally
        {
            vaultSessionCoordinator.SessionStatusChanged -= VaultSessionCoordinatorOnSessionStatusChanged;
        }

        return;
        void OnCancelled()
        {
            tcs.TrySetCanceled();
            uiProcessLauncher.Exit();
        }

        void VaultSessionCoordinatorOnSessionStatusChanged(VaultSessionStatus status)
        {
            if (status == VaultSessionStatus.Unlocked)
            {
                tcs.TrySetResult();
            }
            else
            {
                tcs.TrySetCanceled();
            }
        }
    }
}
