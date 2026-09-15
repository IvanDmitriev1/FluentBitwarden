using FluentBitwarden.AppHost.Infrastructure.Services;
using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.AppHost.Modules.Passkey.Ipc;

internal sealed class PasskeyDialogClient(
    IIpcClient ipcClient,
    IUiProcessLauncher uiProcessLauncher) : IPasskeyDialogClient
{
    public ValueTask<Fido2Credential> ShowPasskeySelectionDialogAsync(
        PasskeySelectCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        uiProcessLauncher.Activate();

        return ipcClient.SendAsync<PasskeySelectCredentialRequest, Fido2Credential>(
            request,
            cancellationToken);
    }
}
