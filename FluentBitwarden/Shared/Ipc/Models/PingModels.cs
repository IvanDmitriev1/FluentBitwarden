namespace FluentBitwarden.Shared.Ipc.Models;

public readonly record struct PingRequest(string Text);

public readonly record struct PingResponse(
    string Text,
    bool Ok);