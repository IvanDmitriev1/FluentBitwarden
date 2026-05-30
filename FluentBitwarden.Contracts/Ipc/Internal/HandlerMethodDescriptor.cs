using FluentBitwarden.Contracts.Ipc.Services;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FluentBitwarden.Contracts.Ipc.Internal;

internal sealed record HandlerMethodDescriptor(
    HandlerMethodKind Kind,
    ushort MessageType,
    MethodInfo Method,
    Type? ResponseType,
    Type? RequestType)
{
    public IIpcRequestHandlerInvoker CreateInvoker<THandler>(THandler handler)
        where THandler : class, IIpcRequestsHandler
    {
        var invokerType = Kind switch
        {
            HandlerMethodKind.RequestResponse =>
                typeof(PipeRequestHandlerInvoker<,,>).MakeGenericType(
                    typeof(THandler),
                    RequestType!,
                    ResponseType!),

            HandlerMethodKind.RequestCommand =>
                typeof(PipeRequestCommandInvoker<,>).MakeGenericType(
                    typeof(THandler),
                    RequestType!),

            HandlerMethodKind.CommandResponse =>
                typeof(PipeCommandHandlerInvoker<,>).MakeGenericType(
                    typeof(THandler),
                    ResponseType!),
            HandlerMethodKind.Command =>
                typeof(PipeCommandHandlerInvoker<>).MakeGenericType(
                    typeof(THandler)),

            _ => throw new InvalidOperationException(
                $"Unsupported IPC handler method kind '{Kind}'.")
        };

        return (IIpcRequestHandlerInvoker)(
            Activator.CreateInstance(invokerType, handler, this)
            ?? throw new InvalidOperationException(
                $"Could not create IPC invoker for method '{Method.Name}'."));
    }


    private static readonly MethodInfo GetMessageTypeMethod =
        typeof(HandlerMethodDescriptor)
            .GetMethod(nameof(GetMessageType), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException(
            $"Could not find {nameof(GetMessageType)}.");

    public static HandlerMethodDescriptor[] Discover<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        THandler>()
        where THandler : class, IIpcRequestsHandler =>
        typeof(THandler)
            .GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly)
            .Where(static method => !method.IsSpecialName)
            .Select(Create)
            .ToArray();

    private static HandlerMethodDescriptor Create(MethodInfo method)
    {
        if (method.IsGenericMethodDefinition)
            throw InvalidSignature(method, "Generic IPC methods are not supported.");

        var parameters = method.GetParameters();
        if (parameters.Length == 2 &&
            typeof(IIpcRequestMessage).IsAssignableFrom(parameters[0].ParameterType) &&
            parameters[1].ParameterType == typeof(CancellationToken))
        {
            var requestType = parameters[0].ParameterType;
            var messageType = (ushort)(GetMessageTypeMethod.MakeGenericMethod(requestType).Invoke(null, null) ??
                                       throw new InvalidOperationException(
                                           $"{requestType.FullName}.{nameof(IIpcRequestMessage.MessageType)} did not return UInt16."));

            if (method.ReturnType == typeof(ValueTask))
            {
                return new HandlerMethodDescriptor(
                    HandlerMethodKind.RequestCommand,
                    messageType,
                    method,
                    ResponseType: null,
                    RequestType: requestType);
            }

            var responseType = method.ReturnType.GetGenericArguments()[0];
            return new HandlerMethodDescriptor(
                HandlerMethodKind.RequestResponse,
                messageType,
                method, 
                responseType,
                requestType);
        }

        if (parameters.Length == 1 &&
            parameters[0].ParameterType == typeof(CancellationToken))
        {
            var attribute = method.GetCustomAttribute<IpcMessageHandlerAttribute>()
                            ?? throw InvalidSignature(
                                method,
                                "A method without a request model must declare [IpcMessage(messageType)].");

            if (method.ReturnType == typeof(ValueTask))
            {
                return new HandlerMethodDescriptor(
                    HandlerMethodKind.Command,
                    attribute.MessageType,
                    method,
                    RequestType: null,
                    ResponseType: null);
            }

            return new HandlerMethodDescriptor(
                HandlerMethodKind.CommandResponse,
                attribute.MessageType,
                method,
                ResponseType: method.ReturnType.GetGenericArguments()[0],
                RequestType: null);
        }

        throw InvalidSignature(
            method,
            "A public IPC method must be one of: " +
            "(TRequest, CancellationToken) returning ValueTask<TResponse>, " +
            "(TRequest, CancellationToken) returning ValueTask, or " +
            "(CancellationToken) returning ValueTask<TResponse>.");
    }

    private static ushort GetMessageType<T>() where T : IIpcRequestMessage => T.MessageType;

    private static InvalidOperationException InvalidSignature(
        MethodInfo method,
        string reason)
    {
        return new InvalidOperationException(
            $"Invalid IPC method '{method.DeclaringType?.FullName}.{method.Name}'. {reason}");
    }
}