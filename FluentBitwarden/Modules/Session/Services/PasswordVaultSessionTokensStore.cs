using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using Windows.Security.Credentials;

namespace FluentBitwarden.Modules.Session.Services;

internal sealed class PasswordVaultSessionTokensStore : ISessionTokensStore
{
    private const string RefreshTokenResource = "MyBitwardenClient.RefreshToken";
    private const string TwoFactorSessionResource = "MyBitwardenClient.TwoFactorSessionToken";

    private readonly PasswordVault _vault = new();

    public void Store(UserId userId, SessionTokens tokens)
    {
        var userIdStr = userId.ToString();

        _vault.Add(new PasswordCredential(RefreshTokenResource, userIdStr, tokens.RefreshToken.Value));

        if (tokens.TwoFactorToken is { } twpFactorToken)
            _vault.Add(new PasswordCredential(TwoFactorSessionResource, userIdStr, twpFactorToken.Value));
    }

    public SessionTokens? Get(UserId userId)
    {
        var userIdStr = userId.ToString();

        if (!TryRetrieveSecret(RefreshTokenResource, userIdStr, out var refreshToken))
            return null;

        if (!TryRetrieveSecret(TwoFactorSessionResource, userIdStr, out var twoFactorToken))
            return new SessionTokens(
                new RefreshToken(refreshToken), 
                null,
                AccessToken.Empty,
                DateTimeOffset.MinValue);

        return new SessionTokens(
            new RefreshToken(refreshToken),
            new TwoFactorToken(twoFactorToken),
            AccessToken.Empty,
            DateTimeOffset.MinValue);
    }

    public void Remove(UserId userId)
    {
        var userIdStr = userId.ToString();

        TryDelete(RefreshTokenResource, userIdStr);
        TryDelete(TwoFactorSessionResource, userIdStr);
    }

    private bool TryRetrieveSecret(string resource, string userName, [NotNullWhen(true)] out string? password)
    {
        password = null;

        try
        {
            var credential = _vault.Retrieve(resource, userName);
            credential.RetrievePassword();

            password = credential.Password;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void TryDelete(string resource, string accountKey)
    {
        try
        {
            var credential = _vault.Retrieve(resource, accountKey);
            _vault.Remove(credential);
        }
        catch
        {
            // Ignore missing credential.
        }
    }

}
