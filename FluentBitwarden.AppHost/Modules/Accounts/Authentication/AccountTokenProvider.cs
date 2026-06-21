using FluentBitwarden.AppHost.Modules.Accounts.Persistence;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Authentication;

internal sealed class AccountTokenProvider(
    IAccountStore accountStore,
    IIdentityApi identityApiClient) : IAccountTokenProvider
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AccountTokens? _currentTokens;

    public async ValueTask<AccountTokens> GetValidTokensAsync(
        AccountProfile account,
        CancellationToken cancellationToken = default)
    {
        var currentTokens = Volatile.Read(ref _currentTokens);
        if (currentTokens is not null && currentTokens.UserId == account.UserId && currentTokens.IsValid())
            return currentTokens;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            currentTokens = Volatile.Read(ref _currentTokens);
            if (currentTokens is null || currentTokens.UserId != account.UserId)
            {
                currentTokens = AccountTokens.Create(
                    account,
                    accountStore.GetRefreshToken(account.UserId));
                Volatile.Write(ref _currentTokens, currentTokens);
            }

            if (currentTokens.IsValid())
                return currentTokens;

            currentTokens = await RefreshSession(currentTokens, cancellationToken);
            Volatile.Write(ref _currentTokens, currentTokens);
            return currentTokens;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<AccountTokens> RefreshSession(
        AccountTokens authenticationTokens,
        CancellationToken cancellationToken)
    {
        var result = await identityApiClient.RefreshAsync(
            new RefreshLoginRequest(
                authenticationTokens.BitwardenClientContext,
                authenticationTokens.RefreshToken),
            cancellationToken);

        if (result is not TokenExchangeOutcome.SessionRefreshed success)
            throw new AccountSessionRefreshException(result);

        var response = success.Session;
        return authenticationTokens with
        {
            RefreshToken = response.RefreshToken,
            AccessToken = response.AccessToken,
            ExpiresAt = response.ExpiresAt
        };
    }
}
