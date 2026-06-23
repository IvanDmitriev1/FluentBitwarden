using FluentBitwarden.Infrastructure.Converters;

namespace FluentBitwarden.ViewModels.Vault.Browse.Models;

public readonly record struct CipherTypeOption(VaultCipherType? Value, string Title) : IOptionItem<VaultCipherType?>
{
    public static readonly CipherTypeOption[] All =
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

public sealed partial class CipherTypeOptionOptionConverter()
    : OptionItemConverter<VaultCipherType?, CipherTypeOption>(CipherTypeOption.All);
