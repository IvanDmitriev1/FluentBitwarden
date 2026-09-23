using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Platform.Ipc.Transport;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;

namespace FluentBitwarden.Infrastructure.Clients;

[Fody.ConfigureAwait(false)]
internal class RemoteWindowsHelloUnlockClient(IIpcClient ipcClient) : IAccountWindowsHelloIntegrationClient
{
    public Task<WindowsHelloEnrollmentStatus> GetEnrollmentAsync(
        GetWindowsHelloEnrollmentRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetWindowsHelloEnrollmentRequest, WindowsHelloEnrollmentStatus>(request, cancellationToken);

    public Task<WindowsHelloEnrollmentOutcome> EnableAsync(
        EnableWindowsHelloEnrollmentRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<EnableWindowsHelloEnrollmentRequest, WindowsHelloEnrollmentOutcome>(request, cancellationToken);

    public async Task DisableAsync(
        DisableWindowsHelloEnrollmentRequest request,
        CancellationToken cancellationToken = default) =>
        await ipcClient.SendAsync<DisableWindowsHelloEnrollmentRequest, IpcVoid>(request, cancellationToken);
}
