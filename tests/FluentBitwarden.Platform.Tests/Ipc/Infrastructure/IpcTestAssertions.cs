using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal static class IpcTestAssertions
{
    public static async Task AssertGenericFailureAsync<TResponse>(
        Func<Task<TResponse>> operation)
    {
        IpcRemoteException exception = await Assert.ThrowsAsync<IpcRemoteException>(
            operation);

        Assert.Equal("The remote IPC request failed.", exception.Message);
        Assert.Null(exception.InnerException);
    }
}
