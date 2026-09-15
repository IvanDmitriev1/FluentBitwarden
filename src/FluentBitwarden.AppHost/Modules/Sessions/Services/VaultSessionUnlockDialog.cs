using FluentBitwarden.AppHost.Infrastructure.Services;
using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.Contracts.Modules.Vault;

namespace FluentBitwarden.AppHost.Modules.Sessions.Services;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSessionUnlockDialog(
    IVaultSessionManager sessionManager,
    IUiProcessLauncher uiProcessLauncher) : IVaultSessionUnlockDialog
{
    public async Task WaitUntilUnlockAsync(CancellationToken cancellationToken)
    {
        if (sessionManager.TryGetUnlockedSession(out var session))
            return;

        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var _ = cancellationToken.Register(OnCancelled);
        sessionManager.SessionStatusChanged += VaultSessionOnSessionStatusChanged;
        uiProcessLauncher.ProcessExited += OnCancelled;

        try
        {
            uiProcessLauncher.Activate();
            await tcs.Task;
        }
        finally
        {
            sessionManager.SessionStatusChanged -= VaultSessionOnSessionStatusChanged;
            uiProcessLauncher.ProcessExited -= OnCancelled;
        }

        return;
        void OnCancelled()
        {
            tcs.TrySetCanceled(cancellationToken);
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
                tcs.TrySetCanceled(CancellationToken.None);
            }
        }
    }
}
