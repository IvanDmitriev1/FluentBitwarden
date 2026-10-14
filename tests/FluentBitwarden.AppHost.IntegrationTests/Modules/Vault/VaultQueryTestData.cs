namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

internal static class VaultQueryTestData
{
    internal static VaultCipherResponse Login(
        string id,
        ReadOnlySpan<byte> key,
        string name,
        string? username = null,
        FolderId folderId = default,
        bool favorite = true,
        DateTimeOffset? creationDate = null,
        DateTimeOffset? revisionDate = null,
        DateTimeOffset? deletedDate = null)
    {
        byte[] payload = VaultParserTestData.WritePayload(key.ToArray(), (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            VaultParserTestData.WriteEncrypted(writer, "name", name, keyBytes);
            if (username is not null)
                VaultParserTestData.WriteEncrypted(writer, "username", username, keyBytes);
            writer.WriteEndObject();
        });

        return CreateResponse(id, VaultCipherType.Login, payload, folderId, favorite, creationDate, revisionDate, deletedDate);
    }

    internal static VaultCipherResponse Card(
        string id,
        ReadOnlySpan<byte> key,
        string name,
        string? cardholderName = null,
        string? brand = null,
        FolderId folderId = default,
        bool favorite = true)
    {
        byte[] payload = VaultParserTestData.WritePayload(key.ToArray(), (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            VaultParserTestData.WriteEncrypted(writer, "name", name, keyBytes);
            if (cardholderName is not null)
                VaultParserTestData.WriteEncrypted(writer, "cardholderName", cardholderName, keyBytes);
            if (brand is not null)
                VaultParserTestData.WriteEncrypted(writer, "brand", brand, keyBytes);
            writer.WriteEndObject();
        });

        return CreateResponse(id, VaultCipherType.Card, payload, folderId, favorite);
    }

    internal static VaultCipherResponse Identity(
        string id,
        ReadOnlySpan<byte> key,
        string name,
        string? fieldName = null,
        string? fieldValue = null)
    {
        byte[] payload = VaultParserTestData.WritePayload(key.ToArray(), (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            VaultParserTestData.WriteEncrypted(writer, "name", name, keyBytes);
            if (fieldName is not null && fieldValue is not null)
                VaultParserTestData.WriteEncrypted(writer, fieldName, fieldValue, keyBytes);
            writer.WriteEndObject();
        });

        return CreateResponse(id, VaultCipherType.Identity, payload);
    }

    private static VaultCipherResponse CreateResponse(
        string id,
        VaultCipherType type,
        byte[] payload,
        FolderId folderId = default,
        bool favorite = true,
        DateTimeOffset? creationDate = null,
        DateTimeOffset? revisionDate = null,
        DateTimeOffset? deletedDate = null)
    {
        VaultCipherResponse template = VaultParserTestData.CreateDto(type, payload);
        return template with
        {
            Id = CipherId.Parse(id),
            FolderId = folderId,
            Favorite = favorite,
            CreationDate = creationDate ?? new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero),
            RevisionDate = revisionDate ?? new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero),
            DeletedDate = deletedDate
        };
    }
}
