using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Infrastructure.Ipc;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNamedPipeIpc(this IServiceCollection services)
    {
        services.AddSingleton<IIpcPipeServer, AppPipeIpcServer>();
        services.AddTransient<IAppPipeIpcClient, AppPipeIpcClient>();
        return services;
    }

    public static IServiceCollection AddPipeMessageHandler<THandler, TRequest, TResponse>(
        this IServiceCollection services)
        where THandler : class, IPipeRequestMessageHandler<TRequest, TResponse>
        where TRequest : IPipeRequestMessage
        where TResponse : notnull
    {
        services.AddTransient<THandler>();

        services.AddSingleton(new PipeMessageInvokerDescriptor(
            TRequest.MessageType,
           static sp =>
           {
               var handler = sp.GetRequiredService<THandler>();
               return new PipeMessageInvoker<THandler, TRequest, TResponse>(handler);
           }));

        return services;
    }
}
