using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.AppHost.Application;

internal sealed class AppHostLifecycleClientHandler(
    IHostApplicationLifetime applicationLifetime) : IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.System.ShutdownAppHost)]
    public ValueTask ShutdownAsync(CancellationToken cancellationToken = default)
    {
        applicationLifetime.StopApplication();
        return ValueTask.CompletedTask;
    }
}
