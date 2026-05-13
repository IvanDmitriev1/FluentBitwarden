using BitwardenApi.Models;
using FluentBitwarden.UI.Converters;

namespace FluentBitwarden.Views.Vault.Models;

public readonly record struct CipherTypeOption(CipherType? Value, string Title) : IOptionItem<CipherType?>
{
    public static readonly CipherTypeOption[] All =
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

public sealed class CipherTypeOptionOptionConverter()
    : OptionItemConverter<CipherType?, CipherTypeOption>(CipherTypeOption.All);
