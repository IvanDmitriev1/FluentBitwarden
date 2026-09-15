using FluentBitwarden.Platform.Ipc.Models;
using System.Reflection;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal static class IpcRpcHandlerMethodDescriptorFactory
{
    [RequiresDynamicCode(
        "IPC handler discovery closes generic message helpers at runtime.")]
    [RequiresUnreferencedCode(
        "IPC handler discovery reflects over handler methods and message metadata.")]
    public static IpcRpcHandlerMethodDescriptor[] Discover<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        THandler>()
        where THandler : class, IIpcRequestsHandler
    {
        return typeof(THandler)
            .GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly)
            .Where(static method => !method.IsSpecialName)
            .Select(Create)
            .ToArray();
    }

    [RequiresDynamicCode(
        "IPC handler discovery closes generic message helpers at runtime.")]
    [RequiresUnreferencedCode(
        "IPC handler discovery reflects over handler methods and message metadata.")]
    private static IpcRpcHandlerMethodDescriptor Create(MethodInfo method)
    {
        if (method.IsGenericMethodDefinition)
            throw InvalidSignature(method, "Generic IPC methods are not supported.");

        var parameters = method.GetParameters();

        if (parameters.Length == 2 &&
            typeof(IIpcRequestMessage).IsAssignableFrom(parameters[0].ParameterType) &&
            parameters[1].ParameterType == typeof(CancellationToken))
        {
            return CreateRequestHandlerDescriptor(method, parameters[0].ParameterType);
        }

        if (parameters.Length == 1 &&
            parameters[0].ParameterType == typeof(CancellationToken))
        {
            return CreateCommandHandlerDescriptor(method);
        }

        throw InvalidSignature(
            method,
            "A public IPC method must be one of: " +
            "(TRequest, CancellationToken) returning ValueTask<TResponse>, " +
            "(TRequest, CancellationToken) returning ValueTask, " +
            "(CancellationToken) returning ValueTask<TResponse>, or " +
            "(CancellationToken) returning ValueTask.");
    }

    [RequiresDynamicCode(
        "IPC handler discovery closes generic message helpers at runtime.")]
    [RequiresUnreferencedCode(
        "IPC handler discovery reflects over handler methods and message metadata.")]
    private static IpcRpcHandlerMethodDescriptor CreateRequestHandlerDescriptor(
        MethodInfo method,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type requestType)
    {
        var messageType = method.GetRequestMessageType(requestType);
        var authRequirement = GetAuthRequirement(method);

        if (method.ReturnType == typeof(ValueTask))
        {
            return new IpcRpcHandlerMethodDescriptor(
                messageType,
                authRequirement,
                IpcRpcHandlerMethodKind.RequestCommand,
                method,
                ResponseType: null,
                RequestType: requestType);
        }

        if (TryGetValueTaskResponseType(method.ReturnType, out var responseType))
        {
            return new IpcRpcHandlerMethodDescriptor(
                messageType,
                authRequirement,
                IpcRpcHandlerMethodKind.RequestResponse,
                method,
                ResponseType: responseType,
                RequestType: requestType);
        }

        throw InvalidSignature(
            method,
            "A request IPC method must return ValueTask or ValueTask<TResponse>.");
    }

    private static IpcRpcHandlerMethodDescriptor CreateCommandHandlerDescriptor(MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<IpcMessageHandlerAttribute>()
                        ?? throw InvalidSignature(method,
                            "A method without a request model must declare [IpcMessageHandler(messageType)].");

        if (attribute.MessageType == 0)
        {
            throw InvalidSignature(
                method,
                "A command method must declare a concrete message type.");
        }

        var messageType = attribute.MessageType;
        var authRequirement = attribute.AuthenticationLevel;

        if (method.ReturnType == typeof(ValueTask))
        {
            return new IpcRpcHandlerMethodDescriptor(
                messageType,
                authRequirement,
                IpcRpcHandlerMethodKind.Command,
                method,
                ResponseType: null,
                RequestType: null);
        }

        if (TryGetValueTaskResponseType(method.ReturnType, out var responseType))
        {
            return new IpcRpcHandlerMethodDescriptor(
                messageType,
                authRequirement,
                IpcRpcHandlerMethodKind.CommandResponse,
                method,
                ResponseType: responseType,
                RequestType: null);
        }

        throw InvalidSignature(
            method,
            "A command IPC method must return ValueTask or ValueTask<TResponse>.");
    }

    private static bool TryGetValueTaskResponseType(
        Type returnType,
        [NotNullWhen(true)] out Type? responseType)
    {
        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            responseType = returnType.GetGenericArguments()[0];
            return true;
        }

        responseType = null;
        return false;
    }

    private static IpcAuthenticationLevel GetAuthRequirement(MethodInfo method) =>
        method.GetCustomAttribute<IpcMessageHandlerAttribute>()?.AuthenticationLevel ??
        IpcAuthenticationLevel.SamePackage;

    private static InvalidOperationException InvalidSignature(MethodInfo method, string reason)
    {
        return new InvalidOperationException(
            $"Invalid IPC method '{method.DeclaringType?.FullName}.{method.Name}'. {reason}");
    }
}
