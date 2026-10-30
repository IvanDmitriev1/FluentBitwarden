using BitwardenApi.Cryptography.Encryption;

namespace BitwardenApi.Vault.Folders.Requests;

public sealed class FolderRequest
{
    public required EncString Name { get; init; }
}
