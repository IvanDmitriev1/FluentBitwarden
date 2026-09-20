using FluentBitwarden.Platform.Ipc.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentBitwarden.Platform.Ipc;

public sealed class IpcRpcHandlerBuilder(IServiceCollection services)
{
    private readonly Dictionary<ushort, (Type HandlerType, IpcRpcEndpoint Endpoint)> _endpoints = [];

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
            if (_endpoints.TryGetValue(descriptor.MessageType, out var existingRegistration))
            {
                throw new InvalidOperationException(
                    $"RPC message type '{descriptor.MessageType}' is already registered by " +
                    $"'{existingRegistration.HandlerType.FullName}' and cannot also be registered by " +
                    $"'{typeof(THandler).FullName}'.");
            }

            _endpoints.Add(
                descriptor.MessageType,
                (typeof(THandler), IpcRpcEndpointFactory.Create<THandler>(descriptor)));
        }

        services.TryAddScoped<THandler>();
        return this;
    }

    internal IReadOnlyDictionary<ushort, IpcRpcEndpoint> Build() =>
        _endpoints.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.Endpoint);
}
