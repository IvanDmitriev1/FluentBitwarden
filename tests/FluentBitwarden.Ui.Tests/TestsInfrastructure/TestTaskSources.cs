namespace FluentBitwarden.Ui.Tests.TestInfrastructure;

internal static class TestTaskSources
{
    public static TaskCompletionSource<T> CreateAsyncContinuationSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
