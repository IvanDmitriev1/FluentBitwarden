namespace FluentBitwarden.BrowserHost.NativeMessaging;

internal readonly record struct NativeMessageReadResult(bool IsEndOfStream, string Json)
{
    public static NativeMessageReadResult EndOfStream { get; } = new(true, string.Empty);

    public static NativeMessageReadResult Message(string json) => new(false, json);
}
