using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.AppHost.Infrastructure.Ipc.Clients;
using FluentBitwarden.AppHost.Infrastructure.Ipc.Handlers;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Ssh;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Infrastructure.Ipc;

internal static class IpcServiceCollectionExtensions
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "IPC registration intentionally reflects over known AppHost handler methods.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "IPC registration intentionally closes known AppHost handler invoker types at startup.")]
    public static IServiceCollection AddAppHostIpc(this IServiceCollection services)
    {
        services.AddIpcServer(IpcConstants.AppHostPipeName, 2);
        services.AddIpcClient(IpcConstants.UiPipeName);

        services.AddSingleton<ISshUserActionDialogClient, SshUserActionDialogClient>();
        services.AddSingleton<IPasskeyCredentialSelectionClient, PasskeyCredentialSelectionClient>();

        services.AddIpcRequestHandler<AccountsIpcHandler>();
        services.AddIpcRequestHandler<WindowsHelloIpcHandler>();
        services.AddIpcRequestHandler<VaultIpcHandler>();
        services.AddIpcRequestHandler<BrowserExtensionIpcHandler>();
        services.AddIpcRequestHandler<PasskeyIpcHandler>();

        return services;
    }
}
