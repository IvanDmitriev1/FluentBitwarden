using FluentBitwarden.Infrastructure.Converters;

namespace FluentBitwarden.ViewModels.Vault.Models;

public readonly record struct VaultCipherTypeOption(VaultCipherType? Value, string Title) : IOptionItem<VaultCipherType?>
{
    public static readonly VaultCipherTypeOption[] All =
    [
        new(null, "All categories"),
        new(VaultCipherType.Login, "Login"),
        new(VaultCipherType.SecureNote, "Secure note"),
        new(VaultCipherType.Card, "Card"),
        new(VaultCipherType.Identity, "Identity"),
        new(VaultCipherType.SshKey, "SSH key"),
    ];

    public override string ToString() => Title;
}

public sealed partial class VaultCipherTypeOptionConverter()
    : OptionItemConverter<VaultCipherType?, VaultCipherTypeOption>(VaultCipherTypeOption.All);
