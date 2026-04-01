using FluentBitwarden.Modules.Security.Abstractions;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Security.Services;

internal sealed class DpApiDataProtection : IDataProtection
{
    public ValueTask<byte[]> ProtectSecret(ReadOnlyMemory<byte> secret, CancellationToken ct = default)
    {
        var result = ProtectedData.Protect(secret.Span, DataProtectionScope.CurrentUser);
        return ValueTask.FromResult(result);
    }

    public ValueTask<byte[]> UnprotectSecretAsync(ReadOnlyMemory<byte> protectedData, CancellationToken ct = default)
    {
        var result = ProtectedData.Unprotect(protectedData.Span, DataProtectionScope.CurrentUser);
        return ValueTask.FromResult(result);
    }
}