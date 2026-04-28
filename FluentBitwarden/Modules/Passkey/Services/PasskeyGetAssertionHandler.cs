using System.Linq;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Passkey.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Shared.Ipc.Abstractions;

namespace FluentBitwarden.Modules.Passkey.Services;

[Fody.ConfigureAwait(false)]
internal class PasskeyGetAssertionHandler(IVaultSyncService service) : IPipeMessageHandler<PasskeyGetAssertionRequest, PasskeyAssertionResponse>
{
    public ushort MessageType => 2;

    public ValueTask<IpcResult<PasskeyAssertionResponse>> HandleAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        var credential = service.Ciphers.OfType<LoginCipher>()
            .SelectMany(static l => l.Fido2Credentials)
            .FirstOrDefault(c => c.RpId == request.RpId);

        if (credential is null)
        {
            return ValueTask.FromResult(IpcResult<PasskeyAssertionResponse>.Fail("Credential not found."));
        }

        return ValueTask.FromResult(
            IpcResult<PasskeyAssertionResponse>.Fail("Passkey get assertion is not implemented."));
    }
}
