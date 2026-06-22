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
        services.AddIpcServer(
            IpcConstants.AppHostPipeName,
            handlers => handlers
                .Add<AccountsIpcHandler>()
                .Add<WindowsHelloIpcHandler>()
                .Add<VaultIpcHandler>()
                .Add<BrowserExtensionIpcHandler>()
                .Add<PasskeyIpcHandler>());
        services.AddIpcClient(IpcConstants.UiPipeName);

        services.AddSingleton<ISshUserActionDialogClient, SshUserActionDialogClient>();
        services.AddSingleton<IPasskeyCredentialSelectionClient, PasskeyCredentialSelectionClient>();

        return services;
    }
}
