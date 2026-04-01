using FluentBitwarden.Modules.Security.Abstractions;

namespace FluentBitwarden.Modules.Security.Services;

internal sealed class WindowsHelloDataProtection : IDataProtection
{
    public ValueTask<byte[]> ProtectSecret(ReadOnlyMemory<byte> secret, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<byte[]> UnprotectSecretAsync(ReadOnlyMemory<byte> protectedData, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}