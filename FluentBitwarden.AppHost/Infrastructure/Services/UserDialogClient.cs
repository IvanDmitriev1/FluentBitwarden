using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Modules.Ssh;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal sealed class UserDialogClient(IIpcClient ipcClient) : IUserDialogClient
{
    private static readonly TimeSpan UiStartupTimeout = TimeSpan.FromMinutes(1);

    public async ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken = default)
    {
        UiProcessLauncher.Activate();

        using var timeOutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeOutTokenSource.CancelAfter(UiStartupTimeout);

        try
        {
            return await ipcClient.SendAsync<SshUserActionRequest, UserActionDialogOutcome>(
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
        finally
        {
            UiProcessLauncher.Exit();
        }
    }
}
