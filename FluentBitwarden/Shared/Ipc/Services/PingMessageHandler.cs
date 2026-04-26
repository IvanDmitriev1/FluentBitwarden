using FluentBitwarden.Shared.Ipc.Abstractions;
using FluentBitwarden.Shared.Ipc.Models;

namespace FluentBitwarden.Shared.Ipc.Services;

public sealed class PingMessageHandler
    : IPipeMessageHandler<PingRequest, PingResponse>
{
    public ushort MessageType => 1;

    public ValueTask<PingResponse> HandleAsync(
        PingRequest request,
        CancellationToken cancellationToken)
    {
        var response = new PingResponse(
            $"pong: {request.Text}",
            Ok: true);

        return ValueTask.FromResult(response);
    }
}