using System.Text.Json.Serialization;




using BitwardenApi.Vault.Ciphers.Responses;
using BitwardenApi.Vault.Collections.Responses;
using BitwardenApi.Vault.Folders.Responses;
namespace BitwardenApi.Vault.Sync.Responses;
public sealed record VaultSyncResponse
{
    [JsonPropertyName("profile")]
    public required VaultProfileResponse Profile { get; init; }

    [JsonPropertyName("folders")]
    public FolderResponse[] Folders { get; init; } = [];

    [JsonPropertyName("collections")]
    public CollectionResponse[] Collections { get; init; } = [];

    [JsonPropertyName("ciphers")]
    public CipherResponse[] VaultCiphers { get; init; } = [];
}
