namespace FluentBitwarden.Ui.Tests.Features.Settings;

internal static class WindowsHelloSettingValueTestSupport
{
    public static TaskCompletionSource<T> NewSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
