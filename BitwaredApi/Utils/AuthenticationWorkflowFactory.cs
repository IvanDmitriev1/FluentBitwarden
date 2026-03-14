using System.Globalization;
using System.Security.Cryptography;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Services;
using BitwaredApi.Utils;

namespace BitwaredApi.Utils;

internal static class AuthenticationWorkflowFactory
{
    public static AuthenticationSuccess CreateAuthenticationSuccess(
        BitwardenEnvironment environment,
        string email,
        string deviceIdentifier,
        TokenResponseModel response,
        byte[]? decryptedUserKey,
        KdfConfigModel? fallbackKdf)
    {
        string? accountId = JwtTokenReader.GetClaim(response.AccessToken, "sub");
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ServerVersionMismatchException("The access token did not include a user id.");
        }

        if (string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            throw new ServerVersionMismatchException("The identity token response did not include a refresh token.");
        }

        string sessionEmail = JwtTokenReader.GetClaim(response.AccessToken, "email")
            ?? CryptoService.NormalizeEmail(email);

        MasterPasswordUnlockModel? masterPasswordUnlock = response.UserDecryptionOptions?.MasterPasswordUnlock;
        string? masterKeyEncryptedUserKey = masterPasswordUnlock?.MasterKeyEncryptedUserKey ?? response.Key;
        KdfConfigModel? masterPasswordKdf = masterPasswordUnlock?.Kdf ?? fallbackKdf ?? response.Kdf;
        string? masterPasswordSalt = masterPasswordUnlock?.Salt;

        AuthSession session = new(
            accountId,
            sessionEmail,
            response.ExpiresAt,
            environment,
            decryptedUserKey is { Length: > 0 });

        PersistableSession persistableSession = new(
            accountId,
            sessionEmail,
            environment,
            response.RefreshToken,
            response.ExpiresAt,
            deviceIdentifier,
            masterKeyEncryptedUserKey,
            response.PrivateKey,
            masterPasswordSalt,
            masterPasswordKdf);

        return new AuthenticationSuccess(
            session,
            persistableSession,
            response.AccessToken,
            decryptedUserKey);
    }

    public static string GetMasterPasswordEncryptedUserKey(this TokenResponseModel response)
        => response.Key
            ?? response.UserDecryptionOptions?.MasterPasswordUnlock?.MasterKeyEncryptedUserKey
            ?? throw new ServerVersionMismatchException("The identity token response did not include a master-password wrapped user key.");

    public static string GenerateAccessCode()
        => RandomNumberGenerator.GetInt32(100_000_000, 999_999_999)
            .ToString(CultureInfo.InvariantCulture);
}
