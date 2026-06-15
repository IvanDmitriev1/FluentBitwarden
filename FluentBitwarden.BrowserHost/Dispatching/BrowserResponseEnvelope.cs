using System.Text.Json;

namespace FluentBitwarden.BrowserHost.Dispatching;

internal sealed record BrowserResponseEnvelope(
    string? Id,
    bool Ok,
    JsonElement? Payload,
    BrowserError? Error)
{
    public static BrowserResponseEnvelope CreateSuccess(string id, JsonElement payload) =>
        new(id, Ok: true, payload, Error: null);

    public static BrowserResponseEnvelope CreateError(string? id, string code, string message) =>
        new(id, Ok: false, Payload: null, new BrowserError(code, message));
}
