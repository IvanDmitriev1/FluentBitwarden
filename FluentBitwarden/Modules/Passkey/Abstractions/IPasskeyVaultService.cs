using FluentBitwarden.Modules.Passkey.Models;

namespace FluentBitwarden.Modules.Passkey.Abstractions;

public interface IPasskeyVaultService
{
    ValueTask<VaultStatusResponse> GetVaultStatusAsync(
        CancellationToken cancellationToken);

    ValueTask<PasskeySignAssertionResponse> SignAssertionAsync(
        PasskeySignAssertionRequest request,
        CancellationToken cancellationToken);
}