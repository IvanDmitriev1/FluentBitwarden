namespace FluentBitwarden.BrowserHost.Dispatching;

internal sealed record BrowserCredentialAvailabilityPayload(
    string? Origin,
    string? Url,
    bool? HasPasswordField);
