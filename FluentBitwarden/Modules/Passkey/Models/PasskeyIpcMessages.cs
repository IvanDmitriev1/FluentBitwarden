namespace FluentBitwarden.Modules.Passkey.Models;

public sealed record VaultStatusRequest;

public sealed record VaultStatusResponse(
    bool IsUnlocked,
    string? UserId);

public sealed record PasskeySignAssertionRequest(
    string OperationId,
    string RpId,
    byte[] CredentialId,
    byte[] ToBeSigned,
    bool UserVerificationRequired);

public sealed record PasskeySignAssertionResponse(
    byte[] Signature,
    long SignCount);