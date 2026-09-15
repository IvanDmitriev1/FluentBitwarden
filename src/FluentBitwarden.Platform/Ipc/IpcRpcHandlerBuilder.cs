using FluentBitwarden.Platform.Ipc.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentBitwarden.Platform.Ipc;

public sealed class IpcRpcHandlerBuilder(IServiceCollection services)
{
    private readonly Dictionary<ushort, (Type HandlerType, Func<IServiceProvider, IpcRpcEndpoint> Create)> _endpoints = [];

    [RequiresDynamicCode("IPC handler registration closes generic invoker types at runtime.")]
    [RequiresUnreferencedCode("IPC handler registration reflects over handler methods and message metadata.")]
    public IpcRpcHandlerBuilder Add<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        THandler>()
        where THandler : class, IIpcRequestsHandler
    {
        foreach (var descriptor in IpcRpcHandlerMethodDescriptorFactory.Discover<THandler>())
        {
            var registration = (
                HandlerType: typeof(THandler),
                Create: (Func<IServiceProvider, IpcRpcEndpoint>)(serviceProvider =>
                    IpcRpcEndpointFactory.Create(
                        serviceProvider.GetRequiredService<THandler>(),
                        descriptor)));

            if (!_endpoints.TryAdd(descriptor.MessageType, registration))
            {
                var existingHandler = _endpoints[descriptor.MessageType].HandlerType;
                throw new InvalidOperationException(
                    $"RPC message type '{descriptor.MessageType}' is already registered by " +
                    $"'{existingHandler.FullName}' and cannot also be registered by " +
                    $"'{typeof(THandler).FullName}'.");
            }
        }

        services.TryAddSingleton<THandler>();
        return this;
    }

    internal IReadOnlyDictionary<ushort, IpcRpcEndpoint> Build(IServiceProvider serviceProvider) =>
        _endpoints.ToDictionary(
            static pair => pair.Key,
            pair => pair.Value.Create(serviceProvider));
}
