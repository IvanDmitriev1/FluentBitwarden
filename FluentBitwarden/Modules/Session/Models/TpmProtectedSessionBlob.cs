namespace FluentBitwarden.Modules.Session.Models;

internal sealed record TpmProtectedSessionBlob(
    int Version,
    byte[] WrappedKey,
    byte[] Nonce,
    byte[] Tag,
    byte[] Ciphertext);
