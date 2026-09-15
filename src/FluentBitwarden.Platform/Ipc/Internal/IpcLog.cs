using Microsoft.Extensions.Logging;

namespace FluentBitwarden.Platform.Ipc.Services;

internal static partial class IpcLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Trace,
        Message = "IPC client disconnected before the server response was delivered.")]
    public static partial void ClientDisconnected(this ILogger logger);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "IPC server loop failed.")]
    public static partial void ServerLoopFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "IPC request handling failed.")]
    public static partial void RequestFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Rejected unauthorized IPC pipe client.")]
    public static partial void UnauthorizedClientRejected(this ILogger logger);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning,
        Message = "Rejected unknown IPC message type {MessageType}.")]
    public static partial void UnknownMessageTypeRejected(this ILogger logger, ushort messageType);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Warning,
        Message = "IPC client rejected. Could not open process {ProcessId}.")]
    public static partial void ClientProcessOpenFailed(this ILogger logger, uint processId);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Warning, Message = "Rejected unauthorized IPC event subscriber.")]
    public static partial void UnauthorizedSubscriberRejected(this ILogger logger);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Trace, Message = "IPC event subscriber registered.")]
    public static partial void SubscriberRegistered(this ILogger logger);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Trace, Message = "IPC event connection closed; reconnecting.")]
    public static partial void EventConnectionClosed(this ILogger logger);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Error, Message = "IPC event client loop failed.")]
    public static partial void EventClientLoopFailed(this ILogger logger, Exception exception);
}
