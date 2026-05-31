using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts.Models;
using System.Security.Cryptography;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.General;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock.Methods;

using AccountUnlockOperationResult = OperationResult<AccountUnlockOutcome, DecryptedUserKey>;

internal sealed class MasterPasswordAccountUnlockMethod
{
    public AccountUnlockOperationResult Unlock(AccountKeyMaterial accountKeyMaterial, string masterPassword)
    {
        try
        {
            var decryptedKey = accountKeyMaterial.EncryptedUserKey.Decrypt(
                masterPassword,
                accountKeyMaterial.Salt,
                accountKeyMaterial.KdfConfig);

            return AccountUnlockOperationResult.WithPayload(new AccountUnlockOutcome.Success(),
                new DecryptedUserKey(accountKeyMaterial.UserId, decryptedKey));
        }
        catch (CryptographicException e)
        {
            return AccountUnlockOperationResult.WithoutPayload(new AccountUnlockOutcome.Failure(e.Message));
        }
    }
}
