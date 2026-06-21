using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Modules.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Modules.SshAgent.Models;
using FluentBitwarden.AppHost.Modules.SshAgent.Models.OpenSsh;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using FluentBitwarden.Contracts.Settings.Models;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Services;

[Fody.ConfigureAwait(false)]
internal sealed class SshKeyProvider(
    IUnlockedVaultReader unlockedVault,
    IVaultSessionCoordinator vaultSessionCoordinator,
    ISshUserActionDialogClient sshUserActionDialogClient) : ISshKeyProvider
{
    private static readonly VaultCipherQuery SshCipherQuery = new() { CipherType = VaultCipherType.SshKey };

    //TODO REMOVE
    private bool HasUnlockedSession => vaultSessionCoordinator.TryGetUnlockedSession(out _);

    public Task<SshIdentityQueryResult> ListIdentitiesAsync(CancellationToken token)
    {
        //TODO implement userDialogClient.Unlock to ask user to unlock vault if it's locked instead of just denying the request
        if (!HasUnlockedSession)
            return Task.FromResult(SshIdentityQueryResult.Denied);

        var data = unlockedVault.GetCiphers(SshCipherQuery).OfType<SshKeyVaultCipher>()
            .Select(static c => new SshPublicIdentityResponce(c.PublicKey.KeyBlob, c.Name))
            .ToList();

        return Task.FromResult(SshIdentityQueryResult.Success(data));
    }

    public async Task<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token)
    {
        if (!HasUnlockedSession || GetShhCipher(request.PublicKeyBlob) is not { } cipher)
            return SshSignatureResult.Failed;

        var userVerificationPolicy = SettingsStore.Instance.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey);
        if (userVerificationPolicy == SensitiveActionPolicy.RequireUserAction)
        {
            var requestDialog = new SshUserActionRequest(
                KeyName: cipher.Name,
                KeyFingerprint: cipher.KeyFingerprint,
                IsForwarded: false);

            var userAction = await sshUserActionDialogClient.ShowSshDialogAsync(requestDialog, token);
            if (userAction == UserActionDialogOutcome.Denied)
                return SshSignatureResult.Failed;
        }

        var privateKey = OpenSshEd25519Key.Parse(cipher.PrivateKey.AsMemory());
        var signedData = privateKey.Sign(request.Data);

        return new SshSignatureResult(OpenSshEd25519Key.AlgorithmName, signedData);
    }

    private SshKeyVaultCipher? GetShhCipher(ReadOnlyMemory<byte> publicKeyBlob)
    {
        return unlockedVault
            .GetCiphers(SshCipherQuery)
            .OfType<SshKeyVaultCipher>()
            .FirstOrDefault(c =>
                c.PublicKey.KeyBlob.SequenceEqual(publicKeyBlob.Span));
    }
}
