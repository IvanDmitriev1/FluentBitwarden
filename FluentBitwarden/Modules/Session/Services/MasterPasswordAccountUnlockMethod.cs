using BitwardenApi.Cryptography;
using BitwardenApi.Models;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Session.Services;

internal sealed class MasterPasswordAccountUnlockMethod : IAccountIUnlockMethod
{
    public UnlockMethodType UnlockMethod => UnlockMethodType.MasterPassword;

    public AccountUnlockOutcome Unlock(AccountKeyMaterial accountKeyMaterial, string masterPassword)
    {
        try
        {
            var decryptedKey = CryptographyService.DecryptUserKey(
                accountKeyMaterial.EncryptedUserKey,
                masterPassword,
                accountKeyMaterial.Salt,
                accountKeyMaterial.KdfConfig);

            return new AccountUnlockOutcome.Success(new DecryptedUserKey(accountKeyMaterial.UserId, decryptedKey));
        }
        catch (CryptographicException e)
        {
            return new AccountUnlockOutcome.Failure(e.Message);
        }
    }
}