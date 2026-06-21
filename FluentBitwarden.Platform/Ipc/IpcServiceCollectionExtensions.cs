using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Platform.Ipc;

public static class IpcServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIpcServer(string pipeName, int maxConcurrentConnections = 1)
        {
            services.AddHostedService(sp =>
                new PipeIpcServer(
                    maxConcurrentConnections,
                    pipeName,
                    sp.GetServices<IpcEndpoint>(),
                    sp.GetRequiredService<IIpcClientsVerifier>()));

            services.AddSingleton<IIpcClientsVerifier, PipeClientsVerifier>();

            return services;
        }

        public IServiceCollection AddIpcClient(string pipeName)
        {
            services.AddSingleton<IIpcClient>(_ => new PipeIpcClient(pipeName));
            return services;
        }

        [RequiresDynamicCode("IPC handler registration closes generic invoker types at runtime.")]
        [RequiresUnreferencedCode("IPC handler registration reflects over handler methods and message metadata.")]
        public IServiceCollection AddIpcRequestHandler<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
            THandler>()
            where THandler : class, IIpcRequestsHandler
        {
            services.AddSingleton<THandler>();

            foreach (var descriptor in IpcEndpointHandlerMethodDescriptorFactory.Discover<THandler>())
            {
                services.AddSingleton<IpcEndpoint>(sp =>
                {
                    var handler = sp.GetRequiredService<THandler>();
                    return IpcEndpointFactory.Create(handler, descriptor);
                });
            }

            return services;
        }
    }
}
