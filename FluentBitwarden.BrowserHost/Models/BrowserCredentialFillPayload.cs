namespace FluentBitwarden.BrowserHost.Models;

internal sealed record BrowserCredentialFillPayload(
    string? ItemId,
    string? Origin,
    string? Url,
    bool? UserGesture);
