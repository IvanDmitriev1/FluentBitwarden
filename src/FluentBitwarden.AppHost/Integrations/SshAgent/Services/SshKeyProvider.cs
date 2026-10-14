using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Settings.Models;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Services;

internal sealed class SshKeyProvider(
    IAppSessionService appSessionService,
    IUiProcessLauncher uiProcessLauncher,
    ISshUserActionDialogClient dialogClient,
    ISettingsStore settingsStore) : ISshKeyProvider
{
    private static readonly VaultCipherQuery SshCipherQuery = new() { CipherType = VaultCipherType.SshKey };

    public IReadOnlyList<SshAgentIdentity> GetIdentities()
    {
        if (appSessionService.State is not AppSessionState.Unlocked)
            return [];

        using var lease = appSessionService.RequireUnlockedSessionLease();

        var data = lease.Vault.GetCiphers(SshCipherQuery).OfType<SshKeyVaultCipher>()
            .Select(static c => new SshAgentIdentity(c.PublicKey.KeyBlob, c.Name))
            .ToList();

        return data;
    }

    public async ValueTask<SshAgentSignature?> GetSignatureAsync(SshAgentSignRequest request, CancellationToken cancellationToken)
    {
        if (appSessionService.State is AppSessionState.NotAuthenticated)
            return null;

        var userVerificationPolicy = settingsStore.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey);
        UserId accountId;
        SshUserActionRequest actionRequest;

        using (var lease = await appSessionService.WaitUntilUnlockedAsync(cancellationToken))
        {
            if (GetCipher(lease.Vault, request.PublicKeyBlob) is not { } cipher)
                return null;

            if (userVerificationPolicy != SensitiveActionPolicy.RequireUserAction)
            {
                uiProcessLauncher.Activate();
                return Ed25519SshSigner.Sign(request, cipher.PrivateKey);
            }

            accountId = lease.Account.UserId;
            actionRequest = new SshUserActionRequest(
                KeyName: cipher.Name,
                KeyFingerprint: cipher.KeyFingerprint,
                IsForwarded: false);
        }

        var userAction = await dialogClient.ShowSshDialogAsync(actionRequest, cancellationToken);
        if (userAction != UserActionDialogOutcome.Approved)
            return null;

        IUnlockedSessionLease activeLease;
        try
        {
            activeLease = appSessionService.RequireUnlockedSessionLease();
        }
        catch (UnlockedSessionRequiredException)
        {
            return null;
        }

        using (activeLease)
        {
            if (activeLease.Account.UserId != accountId ||
                GetCipher(activeLease.Vault, request.PublicKeyBlob) is not { } activeCipher)
            {
                return null;
            }

            uiProcessLauncher.Activate();
            return Ed25519SshSigner.Sign(request, activeCipher.PrivateKey);
        }
    }

    private static SshKeyVaultCipher? GetCipher(IUnlockedVault vault, ReadOnlyMemory<byte> publicKeyBlob) =>
        vault.GetCiphers(SshCipherQuery)
            .OfType<SshKeyVaultCipher>()
            .FirstOrDefault(c => c.PublicKey.KeyBlob.SequenceEqual(publicKeyBlob.Span));
}
