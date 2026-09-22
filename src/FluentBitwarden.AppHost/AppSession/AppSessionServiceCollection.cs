using BitwardenApi.Infrastructure.Transport;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.AppHost.AppSession.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.AppSession;

internal static class AppSessionServiceCollection
{
    public static void AddAppSessionModule(this IServiceCollection services)
    {
        services.AddSingleton<ActiveSessionManager>();

        services.AddScoped<AppSessionService, AppSessionService>();
        services.AddScoped<IBitwardenAccessTokenProvider, SessionAccessTokenProvider>();
    }
}
