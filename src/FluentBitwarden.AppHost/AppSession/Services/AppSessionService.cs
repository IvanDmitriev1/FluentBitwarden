using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class AppSessionService(
    IAccountService accountService,
    IVaultManager vaultManager,
    ActiveSessionManager activeSessionManager) : IAppSessionService
{
    public AppSessionState State => activeSessionManager.State;

    public IUnlockedSessionLease? TryAcquireUnlockedSessionLease() =>
        activeSessionManager.TryAcquireUnlockedSessionLease();

    public ValueTask<IUnlockedSessionLease> WaitUntilUnlockedAsync(CancellationToken cancellationToken)
        => activeSessionManager.WaitUntilUnlockedAsync(cancellationToken);

    public SessionUnlockOutcome Unlock(SessionUnlockRequest request)
    {
        using var sessionTransitionGate = activeSessionManager.TryEnterTransition();
        if (sessionTransitionGate is null)
            return new SessionUnlockOutcome.ConcurrentRequest();

        if (activeSessionManager.State is AppSessionState.Unlocked unlocked)
        {
            return unlocked.Account.UserId == request.UserId
                ? new SessionUnlockOutcome.Success()
                : new SessionUnlockOutcome.Failure(
                    "Lock the current account before unlocking another account.");
        }

        if (accountService.GetAccount(request.UserId) is not { } account)
            return new SessionUnlockOutcome.Failure("Account not found.");

        AccountUnlockMethod method = request switch
        {
            SessionUnlockRequest.MasterPasswordRequest password =>
                new AccountUnlockMethod.MasterPassword(password.MasterPassword),

            SessionUnlockRequest.WindowsHelloRequest hello =>
                new AccountUnlockMethod.WindowsHello(hello.OwnerWindow),

            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        var keyResult = accountService.UnlockKey(request.UserId, method);
        if (keyResult is not AccountKeyUnlockResult.Success { UserKey: { } accountKey })
            return SessionUnlockOutcomeExtensions.ConvertFailure(keyResult, request);

        var unlockedVault = vaultManager.Open(account, accountKey);
        sessionTransitionGate.Unlock(new UnlockedVaultLifetime(account, unlockedVault, accountKey));

        return new SessionUnlockOutcome.Success();
    }

    public async Task LockAsync(CancellationToken cancellationToken = default)
    {
        using var transition = await activeSessionManager.EnterTransition(cancellationToken);
        transition.Lock();
    }
}
