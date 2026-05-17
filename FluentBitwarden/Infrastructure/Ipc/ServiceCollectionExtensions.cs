using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Infrastructure.Ipc;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNamedPipeIpc(this IServiceCollection services)
    {
        services.AddSingleton<IIpcPipeServer, IpcPipeServer>();
        return services;
    }

    public static IServiceCollection AddPipeMessageHandler<THandler, TRequest, TResponse>(
        this IServiceCollection services)
        where THandler : class, IPipeMessageHandler<TRequest, TResponse>
        where TRequest : IPipeRequest<TRequest>
        where TResponse : IPipeMessage<TResponse>
    {
        services.AddTransient<IPipeMessageHandler<TRequest, TResponse>, THandler>();
        services.AddSingleton(new PipeMessageInvokerDescriptor(
            TRequest.MessageType,
            sp =>
            {
                var handler =
                    sp.GetRequiredService<IPipeMessageHandler<TRequest, TResponse>>();

                return new PipeMessageInvoker<TRequest, TResponse>(
                    handler);
            }));

        return services;
    }
}
