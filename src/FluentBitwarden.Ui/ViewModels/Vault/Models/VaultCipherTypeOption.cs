using FluentBitwarden.Infrastructure.Converters;

namespace FluentBitwarden.ViewModels.Vault.Models;

public readonly record struct VaultCipherTypeOption(CipherType? Value, string Title) : IOptionItem<CipherType?>
{
    public static readonly VaultCipherTypeOption[] All =
    [
        new(null, "All categories"),
        new(CipherType.Login, "Login"),
        new(CipherType.SecureNote, "Secure note"),
        new(CipherType.Card, "Card"),
        new(CipherType.Identity, "Identity"),
        new(CipherType.SshKey, "SSH key"),
    ];

    public override string ToString() => Title;
}

public sealed partial class VaultCipherTypeOptionConverter()
    : OptionItemConverter<CipherType?, VaultCipherTypeOption>(VaultCipherTypeOption.All);
