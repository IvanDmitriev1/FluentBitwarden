using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.AppSession;

internal static class AppSessionServiceCollection
{
    public static void AddAppSessionModule(this IServiceCollection services)
    {
        services.AddSingleton<AppSessionStore>();
        services.AddSingleton<IAppSessionLeaseFactory>(static sp => sp.GetRequiredService<AppSessionStore>());

        services.AddScoped<IAppSessionLease>(static sp => sp.GetRequiredService<AppSessionStore>().CreateLease());
        services.AddScoped<IAppSessionOperations, AppSessionOperations>();

        services.AddHostedService<AppSessionHostedService>();
    }
}
