using BitwardenApi.Models;
using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Session.Abstractions;

namespace FluentBitwarden.Infrastructure.IpcClientsImplementations;

[Fody.ConfigureAwait(false)]
internal class RemoteWindowsHelloUnlockClient(IIpcClient ipcClient) : IWindowsHelloUnlockClient
{
    public ValueTask<WindowsHelloStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<WindowsHelloStatus>(
            IpcMessageTypes.WindowsHello.GetCurrentAccountStatus,
            cancellationToken);

    public ValueTask<WindowsHelloStatus> GetStatusAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return ipcClient.SendAsync<GetWindowsHelloStatusRequest, WindowsHelloStatus >(new GetWindowsHelloStatusRequest(userId), cancellationToken);
    }

    public async ValueTask EnableAsync(IntPtr ownerWindowHandle, CancellationToken cancellationToken = default)
    {
        await ipcClient.SendAsync<EnableWindowsHelloRequest, IpcVoid>(new EnableWindowsHelloRequest(ownerWindowHandle),
            cancellationToken);
    }

    public async ValueTask DisableAsync(CancellationToken cancellationToken = default)
    {
        await ipcClient.SendAsync<IpcVoid>(IpcMessageTypes.WindowsHello.Disable, cancellationToken);
    }
}