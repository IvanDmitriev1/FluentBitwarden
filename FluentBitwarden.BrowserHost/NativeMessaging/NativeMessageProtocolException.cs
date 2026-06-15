namespace FluentBitwarden.BrowserHost.NativeMessaging;

internal sealed class NativeMessageProtocolException(
    string code,
    string message,
    bool canContinue = true) : Exception(message)
{
    public string Code { get; } = code;
    public bool CanContinue { get; } = canContinue;
}
