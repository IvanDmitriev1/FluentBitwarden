using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.AppHost.Modules.Passkey.Ipc;

internal sealed class PasskeyIpcHandler(PasskeyAssertionService passkeyAssertionService)
    : IPasskeyClient, IIpcRequestsHandler
{
    public ValueTask<PasskeyAssertionResponse> SelectCredentialAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken) =>
        passkeyAssertionService.SelectCredentialAsync(request, cancellationToken);
}
