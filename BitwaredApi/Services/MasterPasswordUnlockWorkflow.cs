using System.Security.Cryptography;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;

namespace BitwaredApi.Services;

internal sealed class MasterPasswordUnlockWorkflow(ICryptoService cryptoService) : IMasterPasswordUnlockWorkflow
{
    public ValueTask<MasterPasswordUnlockOutcome> UnlockAsync(
        MasterPasswordUnlockRequest request,
        CancellationToken cancellationToken = default)
    {
        PersistableSession session = request.Session;
        if (!session.CanUnlockWithMasterPassword)
        {
            throw new InvalidOperationException("This saved session does not support master-password unlock.");
        }

        using var auth = cryptoService.DeriveMasterPasswordAuth(
            session.Email,
            request.MasterPassword,
            session.KdfConfig!,
            session.MasterPasswordSalt);

        try
        {
            using EncString encryptedUserKey = EncString.From(session.MasterKeyEncryptedUserKey!);
            var userKey = cryptoService.DecryptUserKey(
                encryptedUserKey,
                auth.StretchedMasterKey);

            var protectionKey = SHA256.HashData(auth.StretchedMasterKey);

            MasterPasswordUnlockOutcome outcome = new MasterPasswordUnlockOutcome.Success(
                userKey,
                protectionKey);

            return ValueTask.FromResult(outcome);
        }
        catch (CryptographicException)
        {
            return ValueTask.FromResult<MasterPasswordUnlockOutcome>(
                new MasterPasswordUnlockOutcome.InvalidCredentials("The supplied master password is incorrect."));
        }
    }
}
