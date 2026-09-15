using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.AppHost.Modules.Accounts.Ipc;
using FluentBitwarden.AppHost.Modules.BrowserExtension.Ipc;
using FluentBitwarden.AppHost.Modules.Passkey.Ipc;
using FluentBitwarden.AppHost.Modules.Sessions.Ipc;
using FluentBitwarden.AppHost.Modules.SshAgent.Ipc;
using FluentBitwarden.AppHost.Modules.Vault.Ipc;
using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Platform.Ipc;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost;

internal static class AppHostIpcServiceCollectionExtensions
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
                .Add<SessionIpcHandler>()
                .Add<WindowsHelloIpcHandler>()
                .Add<VaultIpcHandler>()
                .Add<BrowserExtensionIpcHandler>()
                .Add<PasskeyIpcHandler>());
        services.AddIpcEventServer(IpcConstants.AppHostEventsPipeName);
        services.AddIpcClient(IpcConstants.UiPipeName);

        services.AddSingleton<ISshUserActionDialogClient, SshUserActionDialogClient>();
        services.AddSingleton<IPasskeyDialogClient, PasskeyDialogClient>();

        return services;
    }
}
