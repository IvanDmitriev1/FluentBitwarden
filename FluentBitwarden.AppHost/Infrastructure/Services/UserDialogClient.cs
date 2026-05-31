using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Modules.Ssh;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal sealed class UserDialogClient(IIpcClient ipcClient) : IUserDialogClient
{
    private static readonly TimeSpan UiStartupTimeout = TimeSpan.FromSeconds(30);

    public ValueTask<UserActionDialogOutcome> ShowUnlockDialogAsync(
        UnlockVaultUserActionRequest request,
        CancellationToken cancellationToken = default)
        => SendRequest(request, cancellationToken);

    public ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken = default)
        => SendRequest(request, cancellationToken);

    private async ValueTask<UserActionDialogOutcome> SendRequest<TRequest>(TRequest request, CancellationToken cancellationToken) 
        where TRequest : IIpcRequestMessage
    {
        bool uiProcessRunning = UiProcessLauncher.IsRunning();
        if (!uiProcessRunning)
        {
            UiProcessLauncher.ActivateOverlay();
        }

        using var timeOutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeOutTokenSource.CancelAfter(UiStartupTimeout);

        try
        {
            return await ipcClient.SendAsync<TRequest, UserActionDialogOutcome>(
                request,
                timeOutTokenSource.Token);
        }
        catch (OperationCanceledException) when (timeOutTokenSource.Token.IsCancellationRequested)
        {
            return UserActionDialogOutcome.Denied;
        }
        catch (IOException exception)
        {
            Debug.WriteLine($"UI dialog IPC failed: {exception}");
            return UserActionDialogOutcome.Denied;
        }
    }
}
