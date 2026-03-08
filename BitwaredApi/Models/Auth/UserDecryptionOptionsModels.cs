namespace BitwaredApi.Models.Auth;

public sealed record MasterPasswordUnlockModel(
    string Salt,
    KdfConfigModel Kdf,
    string MasterKeyEncryptedUserKey);

public sealed record UserDecryptionOptionsModel(
    bool HasMasterPassword,
    MasterPasswordUnlockModel? MasterPasswordUnlock);
