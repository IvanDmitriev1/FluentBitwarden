using FluentBitwarden.Contracts.Ipc.Internal;
using FluentBitwarden.Contracts.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Contracts.Ipc;

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
