using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Contracts.Modules.Ssh;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal sealed class UserDialogClient(IIpcClient ipcClient) : IUserDialogClient
{
    public ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken = default)
        => SendRequest(request, cancellationToken);

    public ValueTask<Fido2Credential> SelectPasskeyCredential(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        UiProcessLauncher.Activate();

        return ipcClient.SendAsync<PasskeyGetAssertionRequest, Fido2Credential>(
            request,
            cancellationToken);
    }

    private async ValueTask<UserActionDialogOutcome> SendRequest<TRequest>(TRequest request, CancellationToken cancellationToken) 
        where TRequest : IIpcRequestMessage
    {
        UiProcessLauncher.Activate();

        try
        {
            return await ipcClient.SendAsync<TRequest, UserActionDialogOutcome>(
                request,
                cancellationToken);
        }
        catch (Exception)
        {
            return UserActionDialogOutcome.Denied;
        }
    }
}
