using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Identity.Services;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Session.Models.Exceptions;

namespace FluentBitwarden.Modules.Session.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AccountSessionManager(
    IUnitOfWorkFactory unitOfWorkFactory,
    IdentityApiClient identityApiClient,
    IAccountSignInService accountSignInService,
    IAccountSessionTokensStore sessionTokensStore) : IAccountSessionManager, IBitwardenEnvironmentAccessor
{
    public AccountSession? ActiveSession { get; private set; }
    public AccountSession RequireActiveSession => ActiveSession ?? throw new InvalidOperationException("No active session.");

    public BitwardenEnvironment CurrentEnvironment => RequireActiveSession.Context.Environment;

    private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

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

            var result = await identityApiClient.RefreshAsync(
                new RefreshLoginRequest(currentUser.Context, sessionTokens.RefreshToken), cancellationToken);

            if (result is not TokenExchangeOutcome.SessionRefreshed success)
                throw new SessionRefreshException(result);

            var response = success.Session;
            var newSession = new AccountSessionTokens(
                response.RefreshToken,
                response.AccessToken,
                response.ExpiresAt);

            sessionTokensStore.Store(currentUser.UserId, newSession.RefreshToken);
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
            AccountSignInWithPasswordRequest passwordRequest => accountSignInService.SignInWithPasswordAsync(passwordRequest, cancellationToken),
            AccountSignInWithTwoFactorRequest twoFactorRequest => accountSignInService.SignInWithTwoFactorAsync(twoFactorRequest, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        var result = await signInTask;

        if (result is not AccountSignInOutcome.Success successOutcome)
            return result;

        var accountSignIn = successOutcome.AccountSignInSuccess;
        using var unitOfWork = unitOfWorkFactory.Create();

        unitOfWork.AccountProfileRepository.Upsert(new AccountProfile(
            accountSignIn.UserId,
            accountSignIn.Email,
            accountSignIn.Environment,
            LastSyncAt: DateTimeOffset.MinValue));

        unitOfWork.AccountKeyMaterialRepository.Upsert(accountSignIn.AccountKeyMaterial);
        sessionTokensStore.Store(accountSignIn.UserId, accountSignIn.SessionTokens.RefreshToken);

        unitOfWork.SaveChanges();
        return result;
    }

    public Task UnlockAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Lock()
    {
        throw new NotImplementedException();
    }

    public void Logout()
    {
        throw new NotImplementedException();
    }
}