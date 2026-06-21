using FluentBitwarden.AppHost.Modules.Passkey;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.AppHost.Infrastructure.Ipc.Handlers;

internal sealed class PasskeyIpcHandler(PasskeyAssertionService passkeyAssertionService)
    : IPasskeyClient, IIpcRequestsHandler
{
    public ValueTask<PasskeyAssertionResponse> SelectCredentialAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken) =>
        passkeyAssertionService.SelectCredentialAsync(request, cancellationToken);
}
