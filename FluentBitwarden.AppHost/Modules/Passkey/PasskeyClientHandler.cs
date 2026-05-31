using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.AppHost.Modules.Passkey;

internal sealed class PasskeyClientHandler : IPasskeyClient, IIpcRequestsHandler
{
    public ValueTask<Fido2Credential> SelectCredentialAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
