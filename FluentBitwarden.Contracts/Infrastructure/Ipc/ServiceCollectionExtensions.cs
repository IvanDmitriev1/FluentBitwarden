using FluentBitwarden.Contracts.Infrastructure.Ipc.Internal;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Contracts.Infrastructure.Ipc;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIpcServer(string pipeName)
        {
            services.AddHostedService(sp =>
                new PipeIpcServer(pipeName, sp.GetServices<IIpcRequestHandlerInvoker>()));

            return services;
        }

        public IServiceCollection AddIpcClient(string pipeName)
        {
            services.AddSingleton<IIpcClient>(_ => new PipeIpcClient(pipeName));
            return services;
        }

        public IServiceCollection AddIpcRequestHandler<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] THandler>()
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
