using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models.Unlock;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Session.Services.Unlock;

internal readonly record struct MasterPasswordUnlockRequest(string Password) : IUnlockRequest;

internal sealed class MasterPasswordUnlockStrategy : IUnlockStrategy<MasterPasswordUnlockRequest>
{
    public UnlockMethod Method => UnlockMethod.MasterPassword;

    public ValueTask<UnlockResult> UnlockAsync(StoredAccount storedAccount, MasterPasswordUnlockRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var encryptedKey = storedAccount.AccountCryptoMaterial.EncryptedUserKey;
            var decryptedKey = SessionCrypto.DecryptUserKey(
                encryptedKey,
                request.Password,
                storedAccount.Email,
                storedAccount.AccountCryptoMaterial.KdfConfig);

            return ValueTask.FromResult<UnlockResult>(new UnlockResult.Success(new UserKeySession(storedAccount.UserId, Method, decryptedKey)));
        }
        catch (CryptographicException e)
        {
            return ValueTask.FromResult<UnlockResult>(new UnlockResult.Failure(e.Message));
        }
    }
}