using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class AppSessionService(
    IAccountService accountService,
    IVaultManager vaultManager,
    ActiveSessionManager activeSessionManager) : IAppSessionService
{
    public AppSessionSnapshot Snapshot => activeSessionManager.Snapshot;

    public ValueTask<IUnlockedSessionLease> WaitUntilUnlockedAsync(CancellationToken cancellationToken = default)
        => activeSessionManager.WaitUntilUnlockedAsync(cancellationToken);

    public async ValueTask<SessionUnlockOutcome> UnlockAsync(SessionUnlockRequest request, CancellationToken cancellationToken = default)
    {
        using var sessionTransitionGate = activeSessionManager.TryEnterTransition();
        if (sessionTransitionGate is null)
            throw new OperationCanceledException("Concurrent session unlock attempt detected.");

        var currentAccount = activeSessionManager.Snapshot.CurrentAccount;
        if (currentAccount is not null)
        {
            return currentAccount.UserId == request.UserId
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
        sessionTransitionGate.Unlock(new UnlockedSessionState(account, unlockedVault));

        return new SessionUnlockOutcome.Success();
    }

    public async ValueTask LockAsync(CancellationToken cancellationToken = default)
    {
        using var transition = await activeSessionManager.EnterTransition(cancellationToken);
        transition.Lock();
    }
}
