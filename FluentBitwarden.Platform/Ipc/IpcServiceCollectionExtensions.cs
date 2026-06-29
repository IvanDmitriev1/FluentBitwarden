using FluentBitwarden.Platform.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentBitwarden.Platform.Ipc;

public static class IpcServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIpcServer(
            string pipeName,
            Action<IpcRpcHandlerBuilder> configureHandlers)
        {
            var handlers = new IpcRpcHandlerBuilder(services);
            configureHandlers.Invoke(handlers);

            services.AddHostedService(sp =>
                new PipeIpcServer(
                    pipeName,
                    handlers.Build(sp),
                    sp.GetRequiredService<IIpcClientsVerifier>()));

            services.TryAddSingleton<IIpcClientsVerifier, PipeClientsVerifier>();

            return services;
        }

        public IServiceCollection AddIpcEventServer(string pipeName)
        {
            services.AddSingleton(sp =>
                new PipeIpcEventHub(
                    pipeName,
                    sp.GetRequiredService<IIpcClientsVerifier>()));
            services.AddHostedService(static sp => sp.GetRequiredService<PipeIpcEventHub>());
            services.AddSingleton<IIpcEventPublisher>(static sp => sp.GetRequiredService<PipeIpcEventHub>());

            services.TryAddSingleton<IIpcClientsVerifier, PipeClientsVerifier>();

            return services;
        }

        public IServiceCollection AddIpcEventClient(string pipeName)
        {
            services.AddSingleton(_ => new PipeIpcEventClient(pipeName));
            services.AddHostedService(static sp => sp.GetRequiredService<PipeIpcEventClient>());
            services.AddSingleton<IIpcEventClient>(static sp => sp.GetRequiredService<PipeIpcEventClient>());

            return services;
        }

        public IServiceCollection AddIpcClient(string pipeName)
        {
            services.AddSingleton<IIpcClient>(_ => new PipeIpcClient(pipeName));
            return services;
        }
    }
}
