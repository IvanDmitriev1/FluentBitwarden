using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Settings.Models;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Services;

internal sealed class SshKeyProvider(
    IAppSessionAccessor appSessionAccessor,
    IAppSessionOperations appSessionOperations,
    IUserDialogService userDialogService,
    ISettingsStore settingsStore) : ISshKeyProvider
{
    private static readonly VaultCipherQuery SshCipherQuery = new() { CipherType = CipherType.SshKey };

    public async ValueTask<IReadOnlyList<SshAgentIdentity>> GetIdentitiesAsync(CancellationToken cancellationToken)
    {
        await appSessionOperations.WaitUntilUnlockedAsync(cancellationToken);
        var unlocked = appSessionAccessor.RequireUnlockedState();

        var identities = unlocked.Vault.GetCiphers(SshCipherQuery).OfType<SshKeyCipher>()
            .Select(static c => new SshAgentIdentity(c.PublicKey.KeyBlob, c.Name))
            .ToList();
        return identities;
    }

    public async ValueTask<SshAgentSignature?> GetSignatureAsync(SshAgentSignRequest request, CancellationToken cancellationToken)
    {
        var unlocked = appSessionAccessor.RequireUnlockedState();
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, unlocked.Revoked);
        CancellationToken operationToken = operationCancellation.Token;

        var userVerificationPolicy = settingsStore.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey);
        if (GetCipher(unlocked.Vault, request.PublicKeyBlob) is not { } cipher || operationToken.IsCancellationRequested)
            return null;

        if (userVerificationPolicy != SensitiveActionPolicy.RequireUserAction)
        {
            return Ed25519SshSigner.Sign(request, cipher.PrivateKey);
        }

        var actionRequest = new SshUserActionRequest(
            KeyName: cipher.Name,
            KeyFingerprint: cipher.KeyFingerprint,
            IsForwarded: false);

        UserDialogResult<SshUserActionResponse> result =
            await userDialogService.ShowAsync<SshUserActionResponse, SshUserActionRequest>(
                actionRequest, operationToken);

        if (!result.TryGetResult(out var response) || !response.Approved ||
            GetCipher(unlocked.Vault, request.PublicKeyBlob) is not { } activeCipher ||
            operationToken.IsCancellationRequested)
        {
            return null;
        }

        return Ed25519SshSigner.Sign(request, activeCipher.PrivateKey);
    }

    private static SshKeyCipher? GetCipher(IUnlockedVault vault, ReadOnlyMemory<byte> publicKeyBlob) =>
        vault.GetCiphers(SshCipherQuery)
            .OfType<SshKeyCipher>()
            .FirstOrDefault(c => c.PublicKey.KeyBlob.SequenceEqual(publicKeyBlob.Span));
}
