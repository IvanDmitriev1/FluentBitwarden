using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.Contracts.Infrastructure;
using FluentBitwarden.Contracts.Modules.Vault;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSessionUnlockDialog(
    IVaultSession vaultSession,
    IUiProcessLauncher uiProcessLauncher) : IVaultSessionUnlockDialog
{
    public async Task WaitUntilUnlockAsync(CancellationToken cancellationToken)
    {
        if (vaultSession.TryGetUnlockedSession(out var session))
            return;

        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var _ = cancellationToken.Register(OnCancelled);
        vaultSession.SessionStatusChanged += VaultSessionOnSessionStatusChanged;
        uiProcessLauncher.ProcessExited += OnCancelled;

        try
        {
            uiProcessLauncher.Activate();
            await tcs.Task;
        }
        finally
        {
            vaultSession.SessionStatusChanged -= VaultSessionOnSessionStatusChanged;
            uiProcessLauncher.ProcessExited -= OnCancelled;
        }

        return;
        void OnCancelled()
        {
            tcs.TrySetCanceled();
            uiProcessLauncher.Exit();
        }

        void VaultSessionOnSessionStatusChanged(VaultSessionStatus status)
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
