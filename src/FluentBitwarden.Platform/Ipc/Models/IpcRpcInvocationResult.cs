namespace FluentBitwarden.Platform.Ipc.Models;

internal readonly record struct IpcRpcInvocationResult(
    IpcRpcInvocationStatus Status,
    byte[]? ResponsePayload,
    Exception? Exception)
{
    public static IpcRpcInvocationResult Success(byte[] responsePayload) =>
        new(IpcRpcInvocationStatus.Success, responsePayload, null);

    public static IpcRpcInvocationResult Failure(Exception exception) =>
        new(IpcRpcInvocationStatus.Failure, null, exception);

    public static IpcRpcInvocationResult NoResponse =>
        new(IpcRpcInvocationStatus.NoResponse, null, null);
}
