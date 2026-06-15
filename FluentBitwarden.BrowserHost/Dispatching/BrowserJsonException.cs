namespace FluentBitwarden.BrowserHost.Dispatching;

internal sealed class BrowserJsonException(
    string code,
    string message,
    string? id = null) : Exception(message)
{
    public string Code { get; } = code;
    public string? Id { get; } = id;
}
