using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.Application.Abstractions;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class AppSessionOperations(
    IAccountService accountService,
    IVaultService vaultService,
    IAppHostTermination appHostTermination,
    AppSessionStore appSessionStore,
    IServerNotificationsDispatcherFactory notificationsDispatcherFactory) : IAppSessionOperations
{
    public Task AuthenticateAsync(AccountProfile account, CancellationToken cancellationToken) =>
        appSessionStore.AuthenticateAsync(account, cancellationToken);

    public async Task<SessionUnlockOutcome> UnlockAsync(SessionUnlockRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (appSessionStore.State is AppSessionStateSnapshot.Unlocked unlocked)
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
        if (keyResult is not AccountKeyUnlockResult.Success { AccountKeySession: { } accountKey })
            return SessionUnlockOutcomeExtensions.ConvertFailure(keyResult, request);

        AppSessionState.Unlocked unlockedState;
        try
        {
            IUnlockedVault unlockedVault = vaultService.Open(account.BitwardenAccountContext, accountKey);
            var serverNotificationsClient =
                notificationsDispatcherFactory.CreateDispatcher(account.BitwardenAccountContext);

            unlockedState = new AppSessionState.Unlocked(account, unlockedVault, accountKey, serverNotificationsClient);
        }
        catch
        {
            appHostTermination.TerminateApplication();
            throw;
        }

        await appSessionStore.UnlockAsync(unlockedState);
        return new SessionUnlockOutcome.Success();
    }

    public async Task LockAsync(CancellationToken cancellationToken = default)
    {
        await appSessionStore.LockAsync(cancellationToken);
    }
}
