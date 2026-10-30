using System.Text.Json.Serialization;
using BitwardenApi.Cryptography.Encryption;

namespace BitwardenApi.Vault.Folders.Responses;

public struct FolderResponse
{
    public FolderId Id { get; set; }
    public DateTimeOffset RevisionDate { get; set; }

    [JsonPropertyName("name")]
    public EncString EncryptedName { get; set; }
}
