using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Vault.Internal;

internal sealed class RuntimeVaultStore
{
    private readonly Dictionary<string, VaultCipher> _ciphersById =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, VaultFolder> _foldersById =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, VaultCollection> _collectionsById =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Lock _lock = new();


}