using FluentBitwarden.AppHost.Application.Sessions;

namespace FluentBitwarden.AppHost.Modules.Accounts.Authentication;

[Fody.ConfigureAwait(false)]
internal sealed class SignalRAccessTokenProvider(
    IVaultSessionCoordinator vaultSessionCoordinator,
    IAccountTokenProvider tokenProvider) : ISignalRAccessTokenProvider
{
    public async Task<string?> GetAccessToken()
    {
        var session = vaultSessionCoordinator.GetUnlockedSession();
        var sessionTokens = await tokenProvider.GetValidTokensAsync(session.Account, CancellationToken.None);

        return sessionTokens.AccessToken.ToString();
    }
}
