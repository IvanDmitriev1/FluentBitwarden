using BitwardenApi.Cryptography.Keys;

namespace BitwardenApi.Cryptography.KeyDerivation;

/// <summary>
/// The base64-encoded master password hash used only to authenticate to the server.
/// Derived from the <see cref="MasterKey"/>; it never decrypts vault data.
/// </summary>
public readonly record struct MasterPasswordHash(string Value)
{
    public override string ToString() => Value;
}
