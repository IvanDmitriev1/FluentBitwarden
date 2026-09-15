using FluentBitwarden.CommandPalette.Pages;
using FluentBitwarden.CommandPalette.VaultListItems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.CommandPalette.Application;

internal static class CommandPaletteServiceCollectionExtensions
{
    public static IServiceCollection AddCommandPaletteApplicationServices(this IServiceCollection services)
    {
        services.AddHostedService<CommandPaletteComServer>();
        services.AddSingleton<FluentBitwardenCommandsProvider>();
        services.AddSingleton(static services =>
            new FluentBitwardenCommandPaletteExtension(
                services.GetRequiredService<FluentBitwardenCommandsProvider>(),
                services.GetRequiredService<IHostApplicationLifetime>()));
        services.AddSingleton<VaultSearchPage>();
        services.AddSingleton<VaultCipherListItemFactory>();
        services.AddSingleton<UnlockVaultPage>();
        services.AddSingleton<UnlockVaultPage.UnlockFormContent>();

        return services;
    }
}
