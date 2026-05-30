using FluentBitwarden.Contracts.Ipc;
using FluentBitwarden.Modules.Passkey.Abstractions;
using FluentBitwarden.Modules.Passkey.Internal;
using FluentBitwarden.Modules.Passkey.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Passkey.Services;

[Fody.ConfigureAwait(false)]
internal static class PasskeyAssertionHandler
{
    public static IServiceCollection MapPasskeyIpc(this IServiceCollection services)
    {
        /*services.AddIpcRequestHandler<PasskeyGetAssertionRequest, PasskeyAssertionResponse>(static async (
            PasskeyGetAssertionRequest request,
            IPasskeyOverlayService passkeyOverlayService,
            CancellationToken cancellationToken) =>
        {
            var credential = await passkeyOverlayService.UnlockAndSelectAsync(request, cancellationToken);
            var authenticatorData =
                WebAuthnAssertion.BuildAuthenticatorData(request.RpIdHash, credential.Counter, true, true);
            var signedPayload = WebAuthnAssertion.BuildSignedPayload(authenticatorData, request.ClientDataHash);

            var signature = WebAuthnAssertion.SignEs256(
                credential.KeyValue,
                signedPayload);

            var response = new PasskeyAssertionResponse
            {
                CredentialId = credential.CredentialId,
                UserId = credential.UserHandle,
                AuthenticatorData = authenticatorData,
                Signature = signature,
                UserName = credential.UserName,
                UserDisplayName = credential.UserDisplayName
            };

            return response;
        });*/

        return services;
    }
}
