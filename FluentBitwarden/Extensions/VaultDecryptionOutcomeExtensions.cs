using BitwaredApi.Models.Vault;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Extensions;

internal static class VaultDecryptionOutcomeExtensions
{
    public static VaultReadOutcome<T> ToVaultReadOutcome<T>(this VaultDecryptionOutcome<T> outcome)
        => outcome switch
        {
            VaultDecryptionOutcome<T>.Success success => new VaultReadOutcome<T>.Success(success.Value),
            VaultDecryptionOutcome<T>.Failed failed => new VaultReadOutcome<T>.DecryptionFailed(failed.Message),
            _ => throw new InvalidOperationException("Unsupported vault decryption outcome."),
        };

    public static VaultReadOutcome<T?> ToNullableVaultReadOutcome<T>(this VaultDecryptionOutcome<T> outcome)
        where T : class
        => outcome switch
        {
            VaultDecryptionOutcome<T>.Success success => new VaultReadOutcome<T?>.Success(success.Value),
            VaultDecryptionOutcome<T>.Failed failed => new VaultReadOutcome<T?>.DecryptionFailed(failed.Message),
            _ => throw new InvalidOperationException("Unsupported vault decryption outcome."),
        };
}
