using BitwardenApi;
using FluentBitwarden.Application.Lifetime;
using FluentBitwarden.Data;
using FluentBitwarden.Infrastructure.Ipc;
using FluentBitwarden.Infrastructure.Services;
using FluentBitwarden.Modules.Account;
using FluentBitwarden.Modules.Passkey;
using FluentBitwarden.Modules.Session;
using FluentBitwarden.Modules.Session.Services;
using FluentBitwarden.Modules.SshAgent;
using FluentBitwarden.Modules.Vault;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost;

public static class FluentBitwardenApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddFluentBitwardenApplicationServices(this IServiceCollection services)
    {
        services.AddTransient<IAppSetupService, AppSetupService>();

        services.AddNamedPipeIpc();
        services.AddDatabaseServices();
        services.AddApplicationInfrastructureServices();

        services.AddBitwardenApi<BearerAuthTokenProvider>();
        services.AddAccountModule();
        services.AddSessionModule();
        services.AddVaultServices();
        services.AddPasskeyModule();
        services.AddSshAgent();

        return services;
    }
}
