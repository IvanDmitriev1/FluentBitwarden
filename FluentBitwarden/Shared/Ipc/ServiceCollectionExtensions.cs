using FluentBitwarden.Shared.Ipc.Abstractions;
using FluentBitwarden.Shared.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;

namespace FluentBitwarden.Shared.Ipc;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNamedPipeIpc(this IServiceCollection services)
    {
        services.AddSingleton<IIpcPipeServer, IpcPipeServer>();
        return services;
    }

    public static IServiceCollection AddPipeMessageHandler<THandler, TRequest, TResponse>(
        this IServiceCollection services,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo)
        where THandler : class, IPipeMessageHandler<TRequest, TResponse>
        where TRequest : notnull
        where TResponse : notnull
    {
        services.AddTransient<THandler>();

        services.AddTransient<IPipeMessageHandler<TRequest, TResponse>>(sp =>
            sp.GetRequiredService<THandler>());

        services.AddTransient<IPipeMessageInvoker>(sp =>
        {
            var handler = sp.GetRequiredService<IPipeMessageHandler<TRequest, TResponse>>();

            return new PipeMessageInvoker<TRequest, TResponse>(
                handler,
                requestTypeInfo,
                responseTypeInfo);
        });

        return services;
    }
}