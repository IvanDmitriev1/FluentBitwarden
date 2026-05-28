using BitwardenApi.Models;
using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Session.Services;

using AccountUnlockOperationResult = OperationResult<AccountUnlockOutcome, DecryptedUserKey>;

internal sealed class MasterPasswordAccountUnlockMethod : IAccountIUnlockMethod
{
    public UnlockMethodType UnlockMethod => UnlockMethodType.MasterPassword;

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
