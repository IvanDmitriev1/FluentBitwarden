using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Modules.Session.Services;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Security.Services.Unlock;

internal readonly record struct MasterPasswordUnlockRequest(string Password) : IUnlockRequest;

internal sealed class MasterPasswordUnlockStrategy : IUnlockStrategy<MasterPasswordUnlockRequest>
{
    public UnlockMethod Method => UnlockMethod.MasterPassword;

    public ValueTask<UnlockResult> UnlockAsync(AccountDecryption decryption, MasterPasswordUnlockRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var encryptedKey = decryption.EncryptedUserKey;
            var decryptedKey = SessionCrypto.DecryptUserKey(
                encryptedKey,
                request.Password,
                decryption.Salt,
                decryption.KdfConfig);

            return ValueTask.FromResult<UnlockResult>(new UnlockResult.Success(new UserKeySession(decryption.UserId, Method, decryptedKey)));
        }
        catch (CryptographicException e)
        {
            return ValueTask.FromResult<UnlockResult>(new UnlockResult.Failure(e.Message));
        }
    }
}