namespace FluentBitwarden.Modules.Security.Abstractions;

public interface ISecretProtector
{
    void Protect(string filePath, ReadOnlySpan<byte> plaintext);
    byte[]? TryUnprotect(string filePath);
}
