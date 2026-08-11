using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Views.UserDialogs;

namespace FluentBitwarden.Infrastructure.UserDialogs;

internal sealed class PasskeySelectionDialogRequestHandler(
    IVaultClient vaultClient,
    IUiDialogCoordinator dialogCoordinator) : IPasskeyDialogClient, IIpcRequestsHandler
{
    public async ValueTask<Fido2Credential> ShowPasskeySelectionDialogAsync(PasskeySelectCredentialRequest request, CancellationToken cancellationToken = default)
    {
        var credentials = await GetCredentialsAsync(request, cancellationToken);
        return await dialogCoordinator.ShowAsync<Fido2Credential>(new PasskeySelectionDialog(credentials), cancellationToken);
    }

    private async Task<Fido2Credential[]> GetCredentialsAsync(PasskeySelectCredentialRequest request, CancellationToken cancellationToken)
    {
        VaultCipherQuery loginCipherQuery = new() { CipherType = VaultCipherType.Login };

        var ciphers = await vaultClient.SearchCiphersAsync(loginCipherQuery, cancellationToken);
        return ciphers
            .OfType<LoginVaultCipher>()
            .Select(static cipher => cipher.Fido2Credential)
            .OfType<Fido2Credential>()
            .Where(credential => string.Equals(credential.RpId, request.RpId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
