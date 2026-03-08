using System.Security.Cryptography;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Auth;

namespace FluentBitwarden.Services;

internal sealed class LocalVaultSessionUnlocker(
    IAuthService authService,
    ILocalVaultUnlocker localVaultUnlocker)
{
    public async ValueTask<AuthSession> UnlockAsync(
        string accountId,
        byte[] localVaultKey,
        CancellationToken cancellationToken = default)
    {
        byte[]? userKey = null;

        try
        {
            userKey = await localVaultUnlocker.DecryptUserKeyAsync(accountId, localVaultKey, cancellationToken).ConfigureAwait(false);
            return await authService.UnlockWithUserKeyAsync(userKey, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (userKey is not null)
            {
                CryptographicOperations.ZeroMemory(userKey);
            }
        }
    }
}
