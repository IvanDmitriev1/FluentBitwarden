using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Infrastructure.WebAuthn;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Integrations.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class PasskeyIpcHandler(
    IAppSessionAccessor appSessionAccessor,
    IAppSessionOperations appSessionOperations,
    IUserDialogService userDialogService,
    IVaultOrganizationReader vaultOrganizationReader) : IPasskeyClient, IIpcRequestsHandler
{
    private static readonly VaultCipherQuery LoginCipherQuery = new() { CipherType = CipherType.Login };

    public async Task<PasskeyAssertionResponse> SelectCredentialAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        await appSessionOperations.WaitUntilUnlockedAsync(cancellationToken);
        var unlocked = appSessionAccessor.RequireUnlockedState();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, unlocked.Revoked);
        CancellationToken operationToken = cts.Token;

        var offers = new List<(PasskeySelectionCandidate Candidate, byte[] CredentialId)>();
        foreach (var login in unlocked.Vault.GetCiphers(LoginCipherQuery).OfType<LoginCipher>())
        {
            if (login.Fido2Credential is not { } passkey ||
                !string.Equals(passkey.RpId, request.RpId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool isPersonalVault = login.OrganizationId.IsEmpty;
            string? organizationName = isPersonalVault ? null : vaultOrganizationReader.GetOrganizationName(unlocked.Account.UserId, login.OrganizationId);
            string accountName = string.IsNullOrWhiteSpace(passkey.UserName) ? passkey.UserDisplayName : passkey.UserName;

            var candidate = new PasskeySelectionCandidate(
                login.Id, login.Name,
                string.IsNullOrWhiteSpace(accountName) ? null : accountName,
                isPersonalVault, organizationName);
            offers.Add((candidate, passkey.CredentialId.ToArray()));
        }

        if (offers.Count == 0)
            throw new OperationCanceledException(operationToken);

        operationToken.ThrowIfCancellationRequested();
        var result = await userDialogService.ShowAsync<PasskeySelectCredentialResponse, PasskeySelectCredentialRequest>(
            new PasskeySelectCredentialRequest(request.RpId, offers.Select(static offer => offer.Candidate).ToArray()), operationToken);

        operationToken.ThrowIfCancellationRequested();
        CipherId selectedId = result.GetResultOrThrow().CipherId;
        byte[]? offeredCredentialId = offers.Find(offer => offer.Candidate.CipherId == selectedId).CredentialId;
        if (offeredCredentialId is null ||
            unlocked.Vault.GetCipher(selectedId) is not LoginCipher { DeletedDate: null, Fido2Credential: { } credential } ||
            !string.Equals(credential.RpId, request.RpId, StringComparison.OrdinalIgnoreCase) ||
            !credential.CredentialId.AsSpan().SequenceEqual(offeredCredentialId))
        {
            throw new OperationCanceledException(operationToken);
        }

        var (authenticatorData, signature) = WebAuthnAssertion.Create(
            request.RpIdHash, request.ClientDataHash, credential.KeyValue, credential.Counter);

        return new PasskeyAssertionResponse
        {
            CredentialId = credential.CredentialId,
            UserId = credential.UserHandle,
            AuthenticatorData = authenticatorData,
            Signature = signature,
            UserName = credential.UserName,
            UserDisplayName = credential.UserDisplayName
        };
    }

    public Task<PasskeyMakeCredentialResponse> MakeCredentialAsync(PasskeyMakeCredentialRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
