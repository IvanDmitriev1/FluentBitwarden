using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Contracts.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;

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
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRequest, TResponse>(
            Delegate handler)
            where TRequest : IIpcRequestMessage
            where TResponse : notnull
        {
            services.AddSingleton<IIpcRequestHandlerInvoker>(sp =>
            {
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                return new PipeRequestHandlerInvoker<TRequest, TResponse>(scopeFactory, handler);
            });

            return services;
        }

        public IServiceCollection AddIpcRequestHandler<TResponse>(
            ushort messageType,
            Delegate handler)
            where TResponse : notnull
        {
            services.AddSingleton<IIpcRequestHandlerInvoker>(sp =>
            {
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                return new PipeRequestHandlerInvoker<TResponse>(scopeFactory, messageType, handler);
            });

            return services;
        }
    }
}
