using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Infrastructure.Ipc;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIpcServer(string pipeName)
        {
            services.AddHostedService(sp =>
                new AppPipeIpcServer(pipeName, sp.GetServices<IPipeRequestHandlerInvoker>()));

            return services;
        }

        public IServiceCollection AddIpcClient(string pipeName)
        {
            services.AddSingleton<IIpcPipeClient>(_ => new IpcPipeClient(pipeName));
            return services;
        }

        public IServiceCollection AddIpcRequestHandler<THandler, TRequest, TResponse>()
            where THandler : class, IPipeRequestHandler<TRequest, TResponse>
            where TRequest : IPipeRequestMessage
            where TResponse : notnull
        {
            services.AddTransient<THandler>();

            services.AddSingleton<IPipeRequestHandlerInvoker>(sp =>
                new PipeRequestHandlerInvoker<THandler, TRequest, TResponse>(
                    TRequest.MessageType,
                    sp.GetRequiredService<THandler>()));

            return services;
        }
    }
}
