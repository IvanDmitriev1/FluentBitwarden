namespace FluentBitwarden.BrowserHost.Models;

internal sealed record BrowserCredentialAvailabilityPayload(
    string? Origin,
    string? Url,
    bool? HasPasswordField);
