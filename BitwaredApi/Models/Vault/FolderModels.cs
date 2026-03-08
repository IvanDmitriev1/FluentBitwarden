namespace BitwaredApi.Models.Vault;

public sealed record FolderModel(
    string Id,
    string EncryptedJson,
    DateTimeOffset? RevisionDate);
