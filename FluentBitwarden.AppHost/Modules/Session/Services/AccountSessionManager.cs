using BitwardenApi.Contracts;
using BitwardenApi.Models;
using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Contracts.Shared;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Session.Models.Exceptions;

namespace FluentBitwarden.Modules.Session.Services;

using AccountLoginOperationResult = OperationResult<AccountLoginOutcome, AccountSignInSuccess>;
using AccountUnlockOperationResult = OperationResult<AccountUnlockOutcome, DecryptedUserKey>;

[Fody.ConfigureAwait(false)]
internal sealed class AccountSessionManager(
    IUnitOfWorkFactory unitOfWorkFactory,
    IIdentityApiClient identityApiClient,
    IAccountLoginService accountLoginService,
    IAccountSessionTokensStore sessionTokensStore,
    WindowsHelloAccountUnlockMethod windowsHelloAccountUnlockMethod) : IAccountSessionManager, IBitwardenEnvironmentAccessor
{
    public AccountSession? ActiveSession { get; private set; }
    public AccountSession RequireActiveSession => ActiveSession ?? throw new InvalidOperationException("No active session.");
    public BitwardenEnvironment CurrentEnvironment => RequireActiveSession.Context.Environment;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly MasterPasswordAccountUnlockMethod _masterPasswordAccountUnlockMethod = new();

    public async ValueTask<AccountSessionTokens> GetValidActiveSessionTokensAsync(CancellationToken cancellationToken)
    {
        var currentUser = RequireActiveSession;
        var sessionTokens = RequireActiveSession.AccountSessionTokens;
        if (sessionTokens.IsValid())
            return sessionTokens;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            sessionTokens = currentUser.AccountSessionTokens;
            if (sessionTokens.IsValid())
                return sessionTokens;

            var newSession = await RefreshSession(sessionTokens.RefreshToken, currentUser.Context, cancellationToken);
            sessionTokensStore.Store(currentUser.Profile.UserId, newSession.RefreshToken);
            ActiveSession = RequireActiveSession with { AccountSessionTokens = newSession };
            return newSession;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AccountLoginOutcome> LogInAsync(AccountLoginRequest request, CancellationToken cancellationToken)
    {
        Task<AccountLoginOperationResult> signInTask = request switch
        {
            AccountLoginRequest.PasswordRequest passwordRequest => accountLoginService.LoginWithPasswordAsync(passwordRequest, cancellationToken),
            AccountLoginRequest.PasskeyRequest passkeyRequest => accountLoginService.LoginWithPasskeyAsync(passkeyRequest, cancellationToken),
            AccountLoginRequest.TwoFactorRequest twoFactorRequest => accountLoginService.LoginWithTwoFactorAsync(twoFactorRequest, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        var result = await signInTask;


        if (!result.TryGetPayload(out var accountSignIn))
            return result.Outcome;

        using var unitOfWork = unitOfWorkFactory.Create();
        unitOfWork.AccountProfileRepository.Upsert(new AccountProfile(
            accountSignIn.UserId,
            accountSignIn.Email,
            accountSignIn.Environment,
            LastSyncAt: DateTimeOffset.MinValue));

        unitOfWork.AccountKeyMaterialRepository.Upsert(accountSignIn.AccountKeyMaterial);
        unitOfWork.SaveChanges();

        sessionTokensStore.Store(accountSignIn.UserId, accountSignIn.SessionTokens.RefreshToken);
        return new AccountLoginOutcome.Success();
    }

    public AccountProfile[] GetAccounts()
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        return unitOfWork.AccountProfileRepository.GetAccounts();
    }

    public AccountUnlockOutcome Unlock(AccountUnlockRequest request)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var account = request.Account;
        var refreshToken = sessionTokensStore.Get(account.UserId);

        if (unitOfWork.AccountKeyMaterialRepository.GetById(account.UserId) is not { } accountKeyMaterial || refreshToken == RefreshToken.Empty)
            return new AccountUnlockOutcome.RequiresOnlineReauth();

        AccountUnlockOperationResult result = request switch
        {
            AccountUnlockRequest.MasterPasswordRequest masterPasswordRequest => _masterPasswordAccountUnlockMethod.Unlock(accountKeyMaterial, masterPasswordRequest.MasterPassword),
            AccountUnlockRequest.WindowsHelloRequest windowsHelloRequest => windowsHelloAccountUnlockMethod.Unlock(accountKeyMaterial, windowsHelloRequest.OwnerWindowHandle),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        if (!result.TryGetPayload(out var decryptedUserKey))
            return result.Outcome;

        ActiveSession = new AccountSession(
            account,
            new BitwardenClientContext(account.Environment, DeviceIdentity.DeviceInfo),
            AccountSessionTokens.Create(refreshToken), decryptedUserKey, DateTime.UtcNow);

        return new AccountUnlockOutcome.Success();
    }

    public void Lock()
    {
        throw new NotImplementedException();
    }

    public void Logout()
    {
        throw new NotImplementedException();
    }

    private async Task<AccountSessionTokens> RefreshSession(RefreshToken refreshToken, BitwardenClientContext context, CancellationToken cancellationToken)
    {
        var result = await identityApiClient.RefreshAsync(
            new RefreshLoginRequest(context, refreshToken), cancellationToken);

        if (result is not TokenExchangeOutcome.SessionRefreshed success)
            throw new SessionRefreshException(result);

        var response = success.Session;
        var newSession = new AccountSessionTokens(
            response.RefreshToken,
            response.AccessToken,
            response.ExpiresAt);

        return newSession;
    }
}
