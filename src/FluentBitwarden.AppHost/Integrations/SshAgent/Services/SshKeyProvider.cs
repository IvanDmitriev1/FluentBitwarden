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
    IAppSessionLeaseFactory appSessionLeaseFactory,
    IAppSessionLease appSessionLease,
    IUserDialogService userDialogService,
    ISettingsStore settingsStore) : ISshKeyProvider
{
    private static readonly VaultCipherQuery SshCipherQuery = new() { CipherType = CipherType.SshKey };

    public async ValueTask<IReadOnlyList<SshAgentIdentity>> GetIdentitiesAsync(CancellationToken cancellationToken)
    {
        await using var sessionAccessor = await appSessionLeaseFactory.WaitUntilUnlockedAsync(cancellationToken);
        var unlocked = sessionAccessor.RequireUnlockedState();

        var data = unlocked.Vault.GetCiphers(SshCipherQuery).OfType<SshKeyCipher>()
            .Select(static c => new SshAgentIdentity(c.PublicKey.KeyBlob, c.Name))
            .ToList();

        return data;
    }

    public async ValueTask<SshAgentSignature?> GetSignatureAsync(SshAgentSignRequest request, CancellationToken cancellationToken)
    {
        var unlocked = appSessionLease.RequireUnlockedState();
        UserId accountId = unlocked.Account.UserId;

        var userVerificationPolicy = settingsStore.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey);

        if (GetCipher(unlocked.Vault, request.PublicKeyBlob) is not { } cipher)
            return null;

        if (userVerificationPolicy != SensitiveActionPolicy.RequireUserAction)
        {
            return Ed25519SshSigner.Sign(request, cipher.PrivateKey);
        }

        var actionRequest = new SshUserActionRequest(
            KeyName: cipher.Name,
            KeyFingerprint: cipher.KeyFingerprint,
            IsForwarded: false);

        var result = await userDialogService.ShowAsync<SshUserActionResponse, SshUserActionRequest>(actionRequest, cancellationToken);
        if (!result.TryGetResult(out var response) || !response.Approved)
        {
            return null;
        }

        if (unlocked.Account.UserId != accountId ||
            GetCipher(unlocked.Vault, request.PublicKeyBlob) is not { } activeCipher)
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
