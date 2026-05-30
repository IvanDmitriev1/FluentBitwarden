using BitwardenApi.Models;
using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Accounts;
using FluentBitwarden.Contracts.Ipc.Abstractions;

namespace FluentBitwarden.Infrastructure.IpcClientsImplementations;

[Fody.ConfigureAwait(false)]
internal class RemoteWindowsHelloUnlockClient(IIpcClient ipcClient) : IWindowsHelloUnlockClient
{
    public ValueTask<WindowsHelloStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<WindowsHelloStatus>(
            IpcMessageTypes.WindowsHello.GetCurrentAccountStatus,
            cancellationToken);

    public ValueTask<WindowsHelloStatus> GetStatusAsync(GetWindowsHelloStatusRequest request, CancellationToken cancellationToken = default)
    {
        return ipcClient.SendAsync<GetWindowsHelloStatusRequest, WindowsHelloStatus >(request, cancellationToken);
    }

    public async ValueTask EnableAsync(EnableWindowsHelloRequest request, CancellationToken cancellationToken = default)
    {
        await ipcClient.SendAsync<EnableWindowsHelloRequest, IpcVoid>(request, cancellationToken);
    }

    public async ValueTask DisableAsync(CancellationToken cancellationToken = default)
    {
        await ipcClient.SendAsync<IpcVoid>(IpcMessageTypes.WindowsHello.Disable, cancellationToken);
    }
}