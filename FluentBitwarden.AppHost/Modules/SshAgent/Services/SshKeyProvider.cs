using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.SshAgent.Models;
using FluentBitwarden.Contracts.Infrastructure.Shared;
using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Contracts.Modules.AppState.Models;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.SshAgent.Models;
using FluentBitwarden.Modules.SshAgent.Models.OpenSsh;
using FluentBitwarden.Modules.Vault.Abstractions;

namespace FluentBitwarden.Modules.SshAgent.Services;

internal sealed class SshKeyProvider(
    IUnlockedAccountAccessor unlockedAccountAccessor,
    IVaultService vaultService,
    ISshUserActionPrompt userActionPrompt) : ISshKeyProvider
{
    public IReadOnlyList<SshPublicIdentityResponce> ListIdentities() =>
        unlockedAccountAccessor.HasUnlockedAccount ? vaultService.GetAvailableSshKeys() : [];

    public async ValueTask<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token)
    {
        if (!unlockedAccountAccessor.HasUnlockedAccount || vaultService.GetSsh(request.PublicKeyBlob) is not { } cipher)
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
}
