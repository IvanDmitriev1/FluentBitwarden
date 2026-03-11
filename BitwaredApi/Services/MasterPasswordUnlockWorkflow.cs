using System.Security.Cryptography;
using BitwaredApi.Abstractions;
using BitwaredApi.Crypto.Enc;
using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;

namespace BitwaredApi.Services;

internal sealed class MasterPasswordUnlockWorkflow(ICryptoService cryptoService) : IMasterPasswordUnlockWorkflow
{
    public ValueTask<MasterPasswordUnlockOutcome> UnlockAsync(
        MasterPasswordUnlockRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        PersistableSession session = request.Session;
        if (!session.CanUnlockWithMasterPassword)
        {
            throw new InvalidOperationException("This saved session does not support master-password unlock.");
        }

        MasterPasswordAuth auth = cryptoService.DeriveMasterPasswordAuth(
            session.Email,
            request.MasterPassword,
            session.KdfConfig!,
            session.MasterPasswordSalt);

        byte[]? userKey = null;
        byte[]? protectionKey = null;

        try
        {
            userKey = cryptoService.DecryptUserKey(
                new EncString(session.MasterKeyEncryptedUserKey!),
                auth.StretchedMasterKey);

            protectionKey = SHA256.HashData(auth.StretchedMasterKey);

            MasterPasswordUnlockOutcome outcome = new MasterPasswordUnlockOutcome.Success(
                userKey,
                protectionKey);

            userKey = null;
            protectionKey = null;
            return ValueTask.FromResult(outcome);
        }
        catch (CryptographicException)
        {
            return ValueTask.FromResult<MasterPasswordUnlockOutcome>(
                new MasterPasswordUnlockOutcome.InvalidCredentials("The supplied master password is incorrect."));
        }
        finally
        {
            cryptoService.ZeroMemory(userKey);
            cryptoService.ZeroMemory(protectionKey);
            cryptoService.ZeroMemory(auth.MasterKey);
            cryptoService.ZeroMemory(auth.StretchedMasterKey);
        }
    }
}
