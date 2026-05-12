using BitwardenApi.Models;
using System.Collections.Immutable;

namespace FluentBitwarden.Views.Vault.Models;

public readonly record struct CipherTypeOption(
    CipherType? Value,
    string DisplayName)
{
    public static ImmutableArray<CipherTypeOption> All { get; } =
    [
        new(null, "All categories"),
        new(CipherType.Login, "Login"),
        new(CipherType.SecureNote, "Secure note"),
        new(CipherType.Card, "Card"),
        new(CipherType.Identity, "Identity"),
        new(CipherType.SshKey, "SSH key"),
    ];

    public static CipherTypeOption ToCipherTypeOption(CipherType? type) => type switch
    {
        null => All[0],
        CipherType.Login => All[1],
        CipherType.SecureNote => All[2],
        CipherType.Card => All[3],
        CipherType.Identity => All[4],
        CipherType.SshKey => All[5],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static string ToDisplayName(CipherType type) => type switch
    {
        CipherType.Login => "Login",
        CipherType.SecureNote => "Secure note",
        CipherType.Card => "Card",
        CipherType.Identity => "Identity",
        CipherType.SshKey => "SSH key",
        _ => "Unknown"
    };
}