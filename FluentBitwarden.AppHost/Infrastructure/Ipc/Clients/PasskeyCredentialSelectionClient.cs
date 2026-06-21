using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.AppHost.Infrastructure.Ipc.Clients;

internal sealed class PasskeyCredentialSelectionClient(
    IIpcClient ipcClient,
    IUiProcessLauncher uiProcessLauncher) : IPasskeyCredentialSelectionClient
{
    public ValueTask<Fido2Credential> SelectPasskeyCredentialAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        uiProcessLauncher.Activate();

        return ipcClient.SendAsync<PasskeyGetAssertionRequest, Fido2Credential>(
            request,
            cancellationToken);
    }
}
