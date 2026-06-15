namespace FluentBitwarden.BrowserHost.Dispatching;

internal sealed record BrowserCredentialFillPayload(
    string? ItemId,
    string? Origin,
    string? Url,
    bool? UserGesture);
