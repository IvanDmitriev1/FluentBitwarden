using FluentBitwarden.AppHost.Application;
using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.AppHost.Infrastructure.Services;
using FluentBitwarden.Contracts.Infrastructure.Ipc;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Infrastructure;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationInfrastructureServices(this IServiceCollection services)
    {
        services.AddHttpClient("SharedHttpClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.Add("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        });

        services.AddTransient<IAppSetupService, AppSetupService>();

        services.AddIpcServer(IpcConstants.AppHostPipeName, 2);
        services.AddIpcClient(IpcConstants.UiPipeName);
        services.AddIpcRequestHandler<AppHostLifecycleClientHandler>();
        services.AddSingleton<IUserDialogClient, UserDialogClient>();

        return services;
    }
}
