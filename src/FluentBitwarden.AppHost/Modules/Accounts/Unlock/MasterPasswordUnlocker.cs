using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Accounts.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal sealed class MasterPasswordUnlocker
{
    public AccountUnlockResult Unlock(AccountKeyMaterial accountKeyMaterial, string masterPassword)
    {
        try
        {
            var decryptedKey = accountKeyMaterial.ProtectedUserKey.Decrypt(
                masterPassword,
                accountKeyMaterial.Salt,
                accountKeyMaterial.KdfConfig);

            return AccountUnlockResult.WithUserKey(
                new AccountUnlockOutcome.Success(),
                new UserKey(accountKeyMaterial.UserId, decryptedKey));
        }
        catch (CryptographicException exception)
        {
            return AccountUnlockResult.WithoutUserKey(
                new AccountUnlockOutcome.Failure(exception.Message));
        }
    }
}
