namespace FluentBitwarden.AppHost.Modules.Accounts.Persistence;

internal sealed record AccountKeyMaterial(
    UserId UserId,
    string Salt,
    KdfConfig KdfConfig,
    ProtectedUserKey ProtectedUserKey,
    ProtectedPrivateKey ProtectedPrivateKey);
