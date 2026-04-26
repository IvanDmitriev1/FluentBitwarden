using FluentBitwarden.Modules.Passkey.Abstractions;
using FluentBitwarden.Modules.Passkey.Models;

namespace FluentBitwarden.Modules.Passkey.Services;

internal sealed class PasskeyVaultService : IPasskeyVaultService
{
    public ValueTask<VaultStatusResponse> GetVaultStatusAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<PasskeySignAssertionResponse> SignAssertionAsync(PasskeySignAssertionRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}