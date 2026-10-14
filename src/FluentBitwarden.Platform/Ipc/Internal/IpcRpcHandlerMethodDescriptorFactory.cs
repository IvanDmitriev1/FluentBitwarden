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

        throw InvalidSignature(
            method,
            "A public IPC method must declare (TRequest, CancellationToken) " +
            "and return Task<TResponse> or Task.");
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
        if (messageType == 0)
        {
            throw InvalidSignature(method, "The request message type must not be zero.");
        }

        const IpcAuthenticationLevel authRequirement = IpcAuthenticationLevel.SamePackage;

        if (method.ReturnType == typeof(Task))
        {
            return new IpcRpcHandlerMethodDescriptor(
                messageType,
                authRequirement,
                IpcRpcHandlerMethodKind.RequestCommand,
                method,
                ResponseType: null,
                RequestType: requestType);
        }

        if (TryGetTaskResponseType(method.ReturnType, out var responseType))
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
            "A request IPC method must return Task or Task<TResponse>.");
    }

    private static bool TryGetTaskResponseType(
        Type returnType,
        [NotNullWhen(true)] out Type? responseType)
    {
        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            responseType = returnType.GetGenericArguments()[0];
            return true;
        }

        responseType = null;
        return false;
    }

    private static InvalidOperationException InvalidSignature(MethodInfo method, string reason)
    {
        return new InvalidOperationException(
            $"Invalid IPC method '{method.DeclaringType?.FullName}.{method.Name}'. {reason}");
    }
}
