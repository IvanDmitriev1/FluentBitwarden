using FluentBitwarden.AppHost.Modules.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Modules.SshAgent.Models;
using FluentBitwarden.AppHost.Modules.SshAgent.Models.OpenSsh;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.Shared;
using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Contracts.Modules.AppState.Models;
using FluentBitwarden.Contracts.Modules.Vault.Models;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Services;

internal sealed class SshKeyProvider(
    IUnlockedVaultReader unlockedVault,
    ISshUserActionPrompt userActionPrompt) : ISshKeyProvider
{
    private static readonly VaultCipherQuery SshCipherQuery = new() { CipherType = CipherType.SshKey };

    public IReadOnlyList<SshPublicIdentityResponce> ListIdentities()
    {
        if (!unlockedVault.IsOpen)
            return [];

        return unlockedVault.GetCiphers(SshCipherQuery).OfType<SshKeyVaultCipher>()
            .Select(static c => new SshPublicIdentityResponce(c.PublicKey.KeyBlob, c.Name))
            .ToList();
    }

    public async ValueTask<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token)
    {
        if (!unlockedVault.IsOpen || GetShhCipher(request.PublicKeyBlob) is not { } cipher)
            return SshSignatureResult.Failed;

        var userVerificationPolicy = SettingsStore.Instance.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey);
        if (userVerificationPolicy == SensitiveActionPolicy.RequireUserAction)
        {
            var userAction = await userActionPrompt.PromptAsync(
                new SshUserActionRequestViewModel(
                    KeyName: cipher.Name,
                    KeyFingerprint: cipher.KeyFingerprint,
                    IsForwarded: false));

            if (userAction == UserActionDialogOutcome.Denied)
                return SshSignatureResult.Failed;
        }

        var privateKey = OpenSshEd25519Key.Parse(cipher.PrivateKey.AsMemory());
        var signedData = privateKey.Sign(request.Data);

        return new SshSignatureResult(OpenSshEd25519Key.AlgorithmName, signedData);
    }

    private SshKeyVaultCipher? GetShhCipher(ReadOnlyMemory<byte> publicKeyBlob)
    {
        return unlockedVault.GetCiphers(SshCipherQuery).OfType<SshKeyVaultCipher>()
        .FirstOrDefault(c => c.PublicKey.KeyBlob.SequenceEqual(publicKeyBlob.Span));
    }
}
