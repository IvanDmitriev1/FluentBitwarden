using BitwardenApi.Identity;
using BitwardenApi.Infrastructure.Transport;
using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Contracts;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Items;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;

namespace FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Providers;

[Fody.ConfigureAwait(false)]
internal sealed class SignalRAccessTokenProvider(
    IUnlockedAccountAccessor accountAccessor,
    IAccountAuthTokenProvider tokenProvider) : ISignalRAccessTokenProvider
{
    public async Task<string?> GetAccessToken()
    {
        var sessionTokens =
            await tokenProvider.GetValidTokensAsync(accountAccessor.CurrentAccount, CancellationToken.None);

        return sessionTokens.AccessToken.ToString();
    }
}