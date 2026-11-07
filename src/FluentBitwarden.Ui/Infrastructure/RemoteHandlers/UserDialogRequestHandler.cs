using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.Ui.Infrastructure.RemoteHandlers;

internal sealed class UserDialogRequestHandler(
    IVaultCipherClient vaultClient) : IIpcRequestsHandler
{
    /*public Task<UserDialogResult<SshUserActionResponse>> ShowSshDialogAsync(
        SshUserActionRequest request, CancellationToken cancellationToken = default) =>
        presenter.ShowAsync<SshUserActionResponse>(
            () => new SshUserActionDialog(request),
            cancellationToken);


    public async Task<UserDialogResult<Fido2Credential>> ShowPasskeySelectionDialogAsync(
        PasskeySelectCredentialRequest request, CancellationToken cancellationToken = default)
    {
        var credentials = await GetCredentialsAsync(
            request, cancellationToken);

        return await presenter.ShowAsync<Fido2Credential>(
            () => new PasskeySelectionDialog(credentials),
            cancellationToken);
    }*/

    private async Task<Fido2Credential[]> GetCredentialsAsync(PasskeySelectCredentialRequest request, CancellationToken cancellationToken)
    {
        VaultCipherQuery loginCipherQuery = new() { CipherType = CipherType.Login };

        var ciphers = await vaultClient.SearchCiphersAsync(loginCipherQuery, cancellationToken);
        return ciphers
            .OfType<LoginCipher>()
            .Select(static cipher => cipher.Fido2Credential)
            .OfType<Fido2Credential>()
            .Where(credential => string.Equals(credential.RpId, request.RpId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
