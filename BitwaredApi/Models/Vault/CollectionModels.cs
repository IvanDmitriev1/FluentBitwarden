namespace BitwaredApi.Models.Vault;

public sealed record CollectionModel(
    string Id,
    string EncryptedJson,
    DateTimeOffset? RevisionDate);
