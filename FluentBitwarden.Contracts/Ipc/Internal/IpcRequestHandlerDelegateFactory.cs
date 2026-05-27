using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace FluentBitwarden.Contracts.Ipc.Internal;

internal sealed class IpcRequestHandlerDelegateFactory
{
    public static IpcRequestHandlerDelegate<TRequest, TResponse> Create<TRequest, TResponse>(
        Delegate handler)
        where TRequest : IIpcRequestMessage
        where TResponse : notnull
    {
        var parameters = handler.Method.GetParameters();
        ValidateHandler<TRequest, TResponse>(handler, parameters);

        return (request, serviceProvider, cancellationToken) =>
        {
            var arguments = CreateArguments(
                parameters,
                request,
                serviceProvider,
                cancellationToken);

            var result = InvokeHandler(handler, arguments);
            return NormalizeResultAsync<TResponse>(result);
        };
    }

    public static IpcRequestHandlerDelegate<TResponse> Create<TResponse>(
        Delegate handler)
        where TResponse : notnull
    {
        var parameters = handler.Method.GetParameters();
        ValidateHandlerReturnValue<TResponse>(handler);

        return (serviceProvider, cancellationToken) =>
        {
            var arguments = CreateArguments(
                parameters,
                serviceProvider,
                cancellationToken);

            var result = InvokeHandler(handler, arguments);
            return NormalizeResultAsync<TResponse>(result);
        };
    }

    private static void ValidateHandler<TRequest, TResponse>(
        Delegate handler,
        ParameterInfo[] parameters)
        where TRequest : IIpcRequestMessage
        where TResponse : notnull
    {
        var method = handler.Method;

        if (method.ContainsGenericParameters)
        {
            throw new InvalidOperationException(
                "Open generic IPC handlers are not supported.");
        }

        if (parameters.Length < 1 || parameters[0].ParameterType != typeof(TRequest))
        {
            throw new InvalidOperationException(
                $"IPC handler should have at lease once parameter and it should be {nameof(TRequest)}");
        }

        ValidateHandlerReturnValue<TResponse>(handler);
    }

    private static void ValidateHandlerReturnValue<TResponse>(
        Delegate handler)
        where TResponse : notnull
    {
        var method = handler.Method;
        var returnType = method.ReturnType;

        bool validReturnType =
            returnType == typeof(TResponse) ||
            returnType == typeof(Task<TResponse>) ||
            returnType == typeof(ValueTask<TResponse>);

        if (!validReturnType)
        {
            throw new InvalidOperationException(
                $"Invalid IPC handler return type '{returnType.Name}'. " +
                $"Expected {typeof(TResponse).Name}, " +
                $"Task<{typeof(TResponse).Name}>, or " +
                $"ValueTask<{typeof(TResponse).Name}>.");
        }
    }

    private static object?[] CreateArguments<TRequest>(
        ParameterInfo[] parameters,
        TRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TRequest : IIpcRequestMessage
    {
        var arguments = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType;

            if (parameterType == typeof(TRequest))
            {
                arguments[i] = request;
                continue;
            }

            if (parameterType == typeof(CancellationToken))
            {
                arguments[i] = cancellationToken;
                continue;
            }

            arguments[i] = serviceProvider.GetRequiredService(parameterType);
        }

        return arguments;
    }

    private static object?[] CreateArguments(
        ParameterInfo[] parameters,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var arguments = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType;

            if (parameterType == typeof(CancellationToken))
            {
                arguments[i] = cancellationToken;
                continue;
            }

            arguments[i] = serviceProvider.GetRequiredService(parameterType);
        }

        return arguments;
    }

    private static object? InvokeHandler(
        Delegate handler,
        object?[] arguments)
    {
        try
        {
            return handler.DynamicInvoke(arguments);
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static async ValueTask<TResponse> NormalizeResultAsync<TResponse>(
        object? result)
        where TResponse : notnull => result switch
    {
        null => throw new InvalidOperationException($"IPC handler returned null. Expected {typeof(TResponse).Name}."),
        TResponse response => response,
        Task<TResponse> task => await task,
        ValueTask<TResponse> valueTask => await valueTask,
        _ => throw new InvalidOperationException($"Invalid IPC handler return type '{result.GetType().Name}'. " +
                                                 $"Expected {typeof(TResponse).Name}, " +
                                                 $"Task<{typeof(TResponse).Name}>, or " +
                                                 $"ValueTask<{typeof(TResponse).Name}>.")
    };
}