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

        services.AddSingleton<IVaultCipherAttachmentDownloadService, VaultCipherAttachmentDownloadService>();

        return services;
    }
}
