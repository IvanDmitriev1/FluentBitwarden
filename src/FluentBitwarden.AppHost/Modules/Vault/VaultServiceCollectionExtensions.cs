using FluentBitwarden.AppHost.Modules.Vault.Attachments;
using FluentBitwarden.AppHost.Modules.Vault.Workspace;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Vault;

internal static class VaultServiceCollectionExtensions
{
    public static IServiceCollection AddVaultServices(this IServiceCollection services)
    {
        services.AddSingleton<IVaultWorkspace, VaultWorkspace>();

        // Reads for sibling modules, served off whatever session is currently installed. The vault
        // itself lives in the session, so there is no store to register here.
        services.AddSingleton<IUnlockedVaultReader, UnlockedVaultReader>();

        services.AddSingleton<IVaultCipherAttachmentDownloadService, VaultCipherAttachmentDownloadService>();

        return services;
    }
}
