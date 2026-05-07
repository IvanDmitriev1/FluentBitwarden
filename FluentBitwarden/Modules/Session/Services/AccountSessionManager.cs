using BitwardenApi.Modules.Identity.Abstractions;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Infrastructure.Security;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Session.Models.Exceptions;

namespace FluentBitwarden.Modules.Session.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AccountSessionManager(
    IUnitOfWorkFactory unitOfWorkFactory,
    IIdentityApiClient identityApiClient,
    IAccountSignInService accountSignInService,
    IAccountSessionTokensStore sessionTokensStore,
    TpmCngAccountUnlockMethod tpmCngAccountUnlockMethod) : IAccountSessionManager, IBitwardenEnvironmentAccessor
{
    public AccountSession? ActiveSession { get; private set; }
    public AccountSession RequireActiveSession => ActiveSession ?? throw new InvalidOperationException("No active session.");
    public BitwardenEnvironment CurrentEnvironment => RequireActiveSession.Context.Environment;

    private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
    private readonly MasterPasswordAccountUnlockMethod _masterPasswordAccountUnlockMethod = new();
    private readonly TpmCngAccountUnlockMethod _tpmCngAccountUnlockMethod = tpmCngAccountUnlockMethod;

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

    public async Task<AccountSignInOutcome> SignInAsync(AccountSignInRequest request, CancellationToken cancellationToken)
    {
        Task<AccountSignInOutcome> signInTask = request switch
        {
            AccountSignInRequest.PasswordRequest passwordRequest => accountSignInService.SignInWithPasswordAsync(passwordRequest, cancellationToken),
            AccountSignInRequest.TwoFactorRequest twoFactorRequest => accountSignInService.SignInWithTwoFactorAsync(twoFactorRequest, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        var result = await signInTask;

        if (result is not AccountSignInOutcome.Success successOutcome)
            return result;

        var accountSignIn = successOutcome.AccountSignInSuccess;
        using (var unitOfWork = unitOfWorkFactory.Create())
        {
            unitOfWork.AccountProfileRepository.Upsert(new AccountProfile(
                accountSignIn.UserId,
                accountSignIn.Email,
                accountSignIn.Environment,
                LastSyncAt: DateTimeOffset.MinValue,
                AvailableUnlockMethods: UnlockMethodType.MasterPassword));

            unitOfWork.AccountKeyMaterialRepository.Upsert(accountSignIn.AccountKeyMaterial);
            unitOfWork.SaveChanges();
        }

        sessionTokensStore.Store(accountSignIn.UserId, accountSignIn.SessionTokens.RefreshToken);
        return result;
    }

    public IReadOnlyList<AccountProfile> GetAccounts()
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

        AccountUnlockOutcome outcome = request switch
        {
            AccountUnlockRequest.MasterPasswordRequest masterPasswordRequest => _masterPasswordAccountUnlockMethod.Unlock(accountKeyMaterial, masterPasswordRequest.MasterPassword),
            AccountUnlockRequest.TpmCngRequest => _tpmCngAccountUnlockMethod.Unlock(accountKeyMaterial),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        if (outcome is not AccountUnlockOutcome.Success success)
            return outcome;

        ActiveSession = new AccountSession(
            account,
            new BitwardenClientContext(account.Environment, DeviceIdentity.DeviceInfo),
            AccountSessionTokens.Create(refreshToken), success.UserKey, DateTime.UtcNow);

        return outcome;
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
