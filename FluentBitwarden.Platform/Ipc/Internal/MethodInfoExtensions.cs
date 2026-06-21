using System.Reflection;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal static class MethodInfoExtensions
{
    private static readonly MethodInfo GetMessageTypeMethod =
        typeof(MethodInfoExtensions).GetMethod(nameof(GetMessageType), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException(
            $"Could not find {nameof(GetMessageType)}.");

    [RequiresDynamicCode("IPC handler discovery closes generic message helpers at runtime.")]
    [RequiresUnreferencedCode("IPC handler discovery reflects over handler methods and message metadata.")]
    public static ushort GetRequestMessageType(this MethodInfo method, Type requestType)
    {
        var value = GetMessageTypeMethod
            .MakeGenericMethod(requestType)
            .Invoke(null, null);

        return value is ushort messageType
            ? messageType
            : throw new InvalidOperationException(
                $"Invalid IPC method '{method.DeclaringType?.FullName}.{method.Name}'. did not return UInt16");
    }

    private static ushort GetMessageType<T>()
        where T : IIpcRequestMessage => T.MessageType;
}