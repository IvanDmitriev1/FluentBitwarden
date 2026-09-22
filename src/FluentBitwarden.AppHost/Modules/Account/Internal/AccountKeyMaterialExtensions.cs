using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Account.Contracts;

namespace FluentBitwarden.AppHost.Modules.Account.Internal;

internal static class AccountKeyMaterialExtensions
{
    public static AccountKeyUnlockResult MasterPasswordUnlock(this AccountKeyMaterial accountKeyMaterial, string masterPassword)
    {
        try
        {
            var decryptedKey = accountKeyMaterial.ProtectedUserKey.Decrypt(
                masterPassword,
                accountKeyMaterial.Salt,
                accountKeyMaterial.KdfConfig);

            return new AccountKeyUnlockResult.Success(new UnlockedUserKey(accountKeyMaterial.UserId, decryptedKey));

        }
        catch (CryptographicException exception)
        {
            return new AccountKeyUnlockResult.Failure(exception.Message);
        }
    }
}
