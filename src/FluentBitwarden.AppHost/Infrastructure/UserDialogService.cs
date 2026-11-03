using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Infrastructure;

internal sealed class UserDialogService(
    IIpcClient ipcClient,
    IUiProcessLauncher uiProcessLauncher)
    : IUserDialogService
{
    private readonly SemaphoreSlim _presentationGate = new(1, 1);


    public async Task<UserDialogResult<TResult>> ShowAsync<TResult, TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TResult : notnull where TRequest : IIpcRequestMessage
    {
        await _presentationGate.WaitAsync(cancellationToken);

        try
        {
            uiProcessLauncher.Activate();

            var result = await ipcClient.SendAsync<TRequest, UserDialogResult<TResult>>(request, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            return UserDialogResult<TResult>.Cancelled;
        }
        finally
        {
            _presentationGate.Release();
        }
    }
}
