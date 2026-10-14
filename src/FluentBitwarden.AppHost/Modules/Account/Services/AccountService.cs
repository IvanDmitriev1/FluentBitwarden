using BitwardenApi.Identity;
using BitwardenApi.Identity.Contracts;
using BitwardenApi.Primitives;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Internal;
using FluentBitwarden.AppHost.Modules.Account.Persistance;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Platform.Infrastructure;

namespace FluentBitwarden.AppHost.Modules.Account.Services;

internal sealed class AccountService(
    IUnitOfWork unitOfWork,
    IAccountWindowsHelloService accountWindowsHelloService,
    IIdentityApi identityApi,
    AccountAuthenticatorService accountAuthenticatorService,
    AccountKeyMaterialRepository accountKeyMaterialRepository,
    AccountBitwardenSessionTokenRepository accountBitwardenSessionTokenRepository,
    AccountProfileRepository accountProfileRepository) : IAccountService
{
    public AccountProfile[] GetAccounts() => accountProfileRepository.GetAccounts();

    public AccountProfile? GetAccount(UserId userId) => accountProfileRepository.GetById(userId);

    public async Task<AccountAuthenticationOutcome> AuthenticateAsync(AccountAuthenticationRequest request, CancellationToken cancellationToken)
    {
        var result = await accountAuthenticatorService.AuthenticateAsync(request, cancellationToken);
        if (result is AccountAuthenticatorService.Result.Rejected rejected)
            return rejected.Outcome;

        (AccountProfile profile, AccountKeyMaterial keyMaterial, SessionRefreshToken refreshToken) = (AccountAuthenticatorService.Result.Authenticated)result;
        unitOfWork.Begin();

        accountProfileRepository.Upsert(profile);
        accountKeyMaterialRepository.Upsert(keyMaterial);
        accountBitwardenSessionTokenRepository.Store(profile.UserId, refreshToken);

        unitOfWork.Commit();

        return new AccountAuthenticationOutcome.Success(profile);
    }

    public async Task<AccountSessionTokens> RefreshSessionTokens(BitwardenAccountContext accountContext, CancellationToken cancellationToken)
    {
        var refreshToken = accountBitwardenSessionTokenRepository.Get(accountContext.UserId);
        var result = await identityApi.RefreshAuthenticationAsync(new RefreshAuthenticationRequest(new BitwardenClientContext(accountContext.Environment, DeviceIdentity.DeviceInfo), refreshToken), cancellationToken);
        if (result is SessionTokenResult<TokenRefreshSessionModel>.Rejected rejected)
            throw new AccountSessionRefreshException(rejected);

        var success = (SessionTokenResult<TokenRefreshSessionModel>.Success)result;
        var sessionRefreshModel = success.Value;

        unitOfWork.Begin();
        accountBitwardenSessionTokenRepository.Store(accountContext.UserId, sessionRefreshModel.SessionRefreshToken);
        unitOfWork.Commit();

        return new AccountSessionTokens(
            UserId: accountContext.UserId,
            ClientContext: new BitwardenClientContext(accountContext.Environment, DeviceIdentity.DeviceInfo),
            RefreshToken: sessionRefreshModel.SessionRefreshToken,
            AccessToken: sessionRefreshModel.SessionAccessToken,
            ExpiresAt: sessionRefreshModel.ExpiresAt
        );
    }

    public AccountKeyUnlockResult UnlockKey(UserId userId, AccountUnlockMethod method)
    {
        if (accountKeyMaterialRepository.GetById(userId) is not { } accountKeyMaterial)
            return new AccountKeyUnlockResult.RequiresOnlineReauthentication();

        return method switch
        {
            AccountUnlockMethod.MasterPassword masterPassword =>
                accountKeyMaterial.MasterPasswordUnlock(masterPassword.Password),
            AccountUnlockMethod.WindowsHello windowsHello =>
                accountWindowsHelloService.Unlock(accountKeyMaterial, (IntPtr)windowsHello.OwnerWindow.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }
}
