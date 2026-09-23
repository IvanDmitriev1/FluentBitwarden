namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal static class IpcTestAssertions
{
    public static async Task AssertGenericFailureAsync<TResponse>(
        Func<Task<TResponse>> operation)
    {
        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(
            operation);

        Assert.Equal(new OperationCanceledException().Message, exception.Message);
        Assert.Null(exception.InnerException);
    }
}
