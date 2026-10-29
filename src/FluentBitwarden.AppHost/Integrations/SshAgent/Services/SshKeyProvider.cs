using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Settings.Models;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Services;

internal sealed class SshKeyProvider(
    IAppSessionLeaseProvider sessionLeaseProvider,
    IUiProcessLauncher uiProcessLauncher,
    ISshUserActionDialogClient dialogClient,
    ISettingsStore settingsStore) : ISshKeyProvider
{
    private static readonly VaultCipherQuery SshCipherQuery = new() { CipherType = CipherType.SshKey };

    public IReadOnlyList<SshAgentIdentity> GetIdentities()
    {
        using var lease = sessionLeaseProvider.CreateLease();
        if (lease.AppSessionState is not AppSessionState.Unlocked unlocked)
            return [];

        var data = unlocked.Vault.GetCiphers(SshCipherQuery).OfType<SshKeyCipher>()
            .Select(static c => new SshAgentIdentity(c.PublicKey.KeyBlob, c.Name))
            .ToList();

        return data;
    }

    public async ValueTask<SshAgentSignature?> GetSignatureAsync(SshAgentSignRequest request, CancellationToken cancellationToken)
    {
        using (var lease = sessionLeaseProvider.CreateLease())
        {
            if (lease.AppSessionState is AppSessionState.NotAuthenticated)
                return null;
        }

        var userVerificationPolicy = settingsStore.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey);
        UserId accountId;
        SshUserActionRequest actionRequest;

        using (var lease = await sessionLeaseProvider.WaitUntilUnlockedAsync(cancellationToken))
        {
            var unlocked = lease.RequireUnlockedState();

            if (GetCipher(unlocked.Vault, request.PublicKeyBlob) is not { } cipher)
                return null;

            if (userVerificationPolicy != SensitiveActionPolicy.RequireUserAction)
            {
                uiProcessLauncher.Activate();
                return Ed25519SshSigner.Sign(request, cipher.PrivateKey);
            }

            accountId = unlocked.Account.UserId;
            actionRequest = new SshUserActionRequest(
                KeyName: cipher.Name,
                KeyFingerprint: cipher.KeyFingerprint,
                IsForwarded: false);
        }

        var userAction = await dialogClient.ShowSshDialogAsync(actionRequest, cancellationToken);
        if (userAction != UserActionDialogOutcome.Approved)
            return null;

        using var activeLease = sessionLeaseProvider.CreateLease();
        if (activeLease.AppSessionState is not AppSessionState.Unlocked unlockedAfterDialog)
            return null;

        if (unlockedAfterDialog.Account.UserId != accountId ||
            GetCipher(unlockedAfterDialog.Vault, request.PublicKeyBlob) is not { } activeCipher)
        {
            return null;
        }

        uiProcessLauncher.Activate();
        return Ed25519SshSigner.Sign(request, activeCipher.PrivateKey);
    }

    private static SshKeyCipher? GetCipher(IUnlockedVault vault, ReadOnlyMemory<byte> publicKeyBlob) =>
        vault.GetCiphers(SshCipherQuery)
            .OfType<SshKeyCipher>()
            .FirstOrDefault(c => c.PublicKey.KeyBlob.SequenceEqual(publicKeyBlob.Span));
}
