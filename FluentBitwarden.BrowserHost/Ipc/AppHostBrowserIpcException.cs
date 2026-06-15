namespace FluentBitwarden.BrowserHost.Ipc;

internal sealed class AppHostBrowserIpcException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}
