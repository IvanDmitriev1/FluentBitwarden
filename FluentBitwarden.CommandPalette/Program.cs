using FluentBitwarden.CommandPalette.Pages;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Platform.SiteIcons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.CommandPalette;

public static class Program
{
    private const string ComServerArgument = "-RegisterProcessAsComServer";

    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 0 || !StringComparer.Ordinal.Equals(args[0], ComServerArgument))
            return;

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddHostedService<CommandPaletteComServer>();

        builder.Services.AddIpcClient(IpcConstants.AppHostPipeName);
        builder.Services.AddIpcEventClient(IpcConstants.AppHostEventsPipeName);
        builder.Services.AddSingleton<IAccountsClient, RemoteAccountsClient>();
        builder.Services.AddSingleton<IVaultClient, RemoteVaultClient>();
        builder.Services.AddSiteIconCache();

        builder.Services.AddSingleton<FluentBitwardenCommandsProvider>();
        builder.Services.AddSingleton(static services =>
            new FluentBitwardenCommandPaletteExtension(
                services.GetRequiredService<FluentBitwardenCommandsProvider>(),
                services.GetRequiredService<IHostApplicationLifetime>()));

        builder.Services.AddSingleton<VaultCipherListItemFactory>();
        builder.Services.AddSingleton<VaultSearchPage>();

        using var host = builder.Build();
        host.Run();
    }
}
