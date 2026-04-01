namespace FluentBitwarden.Modules.Security.Abstractions;

internal interface IDataProtection
{
    ValueTask<byte[]> ProtectSecret(ReadOnlyMemory<byte> secret, CancellationToken ct = default);
    ValueTask<byte[]> UnprotectSecretAsync(ReadOnlyMemory<byte> protectedData, CancellationToken ct = default);
}