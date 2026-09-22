namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal static class TestTaskCompletionSource
{
    public static TaskCompletionSource<T> Create<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
