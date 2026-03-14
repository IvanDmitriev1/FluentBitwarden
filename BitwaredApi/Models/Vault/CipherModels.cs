namespace BitwaredApi.Models.Vault;

public enum CipherType
{
    Login = 1,
    SecureNote = 2,
    Card = 3,
    Identity = 4,
    SshKey = 5,
}

public sealed record DecryptedCustomField(
    string? Name,
    string? Value,
    int? Type);

public sealed record DecryptedCipher(
    string Id,
    CipherType Type,
    string? Name,
    string? Username,
    string? Password,
    string? Notes,
    IReadOnlyList<string> Uris,
    IReadOnlyList<DecryptedCustomField> Fields,
    string? FolderId,
    string? OrganizationId,
    DateTimeOffset? RevisionDate);
