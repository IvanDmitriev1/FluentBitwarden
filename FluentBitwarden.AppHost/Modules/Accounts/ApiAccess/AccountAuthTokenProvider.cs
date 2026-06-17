using BitwardenApi.Identity;
using BitwardenApi.Infrastructure.Transport;
using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Contracts;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Items;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Models;
using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Models.Exceptions;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.ApiAccess;

internal sealed class AccountAuthTokenProvider(
    IUnitOfWorkFactory unitOfWorkFactory,
    IIdentityApi identityApiClient) : IAccountAuthTokenProvider
{
    private AccountAuthenticationTokens? _currentTokens;

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async ValueTask<AccountAuthenticationTokens> GetValidTokensAsync(AccountProfile account, CancellationToken ct = default)
    {
        if (_currentTokens is not null && _currentTokens.UserId == account.UserId && _currentTokens.IsValid())
            return _currentTokens;

        await _gate.WaitAsync(ct);
        try
        {
            if (_currentTokens is null)
            {
                using var unitOfWork = unitOfWorkFactory.Create();

                _currentTokens = AccountAuthenticationTokens.Create(
                    account,
                    unitOfWork.SecureRefreshTokenStore.Get(account.UserId));
            }

            if (_currentTokens.IsValid())
                return _currentTokens;

            _currentTokens = await RefreshSession(_currentTokens, ct);
            return _currentTokens;
        }
        finally
        {
            _gate.Release();
        }

    }

    private async Task<AccountAuthenticationTokens> RefreshSession(
        AccountAuthenticationTokens authenticationTokens,
        CancellationToken cancellationToken)
    {
        var result = await identityApiClient.RefreshAsync(
            new RefreshLoginRequest(authenticationTokens.BitwardenClientContext, authenticationTokens.RefreshToken), cancellationToken);

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