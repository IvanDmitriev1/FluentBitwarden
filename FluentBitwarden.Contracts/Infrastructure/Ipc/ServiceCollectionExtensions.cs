using FluentBitwarden.Contracts.Infrastructure.Ipc.Internal;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Contracts.Infrastructure.Ipc;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIpcServer(string pipeName, int maxConcurrentConnections = 1)
        {
            services.AddHostedService(sp =>
                new PipeIpcServer(
                    maxConcurrentConnections,
                    pipeName,
                    sp.GetServices<IIpcRequestHandlerInvoker>()));

            return services;
        }

        public IServiceCollection AddIpcClient(string pipeName)
        {
            services.AddSingleton<IIpcClient>(_ => new PipeIpcClient(pipeName));
            return services;
        }

        [RequiresDynamicCode(
            "IPC handler registration closes generic invoker types at runtime.")]
        [RequiresUnreferencedCode(
            "IPC handler registration reflects over handler methods and message metadata.")]
        public IServiceCollection AddIpcRequestHandler<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicConstructors |
                DynamicallyAccessedMemberTypes.PublicMethods)]
        THandler>()
            where THandler : class, IIpcRequestsHandler
        {
            services.AddSingleton<THandler>();

            foreach (var descriptor in HandlerMethodDescriptor.Discover<THandler>())
            {
                services.AddSingleton<IIpcRequestHandlerInvoker>(sp =>
                    descriptor.CreateInvoker(sp.GetRequiredService<THandler>()));
            }

            return services;
        }


    }
}
