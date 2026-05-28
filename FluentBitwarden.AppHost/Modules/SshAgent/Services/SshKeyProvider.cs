using BitwardenApi.Models;
using FluentBitwarden.Contracts.AppState;
using FluentBitwarden.Contracts.AppState.Models;
using FluentBitwarden.Contracts.Shared;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.SshAgent.Models;
using FluentBitwarden.Modules.SshAgent.Models.OpenSsh;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Resources.Dialogs.Models;

namespace FluentBitwarden.Modules.SshAgent.Services;

internal sealed class SshKeyProvider(
    IAccountSessionManager accountSessionManager,
    IVaultService vaultService,
    ISshUserActionPrompt userActionPrompt) : ISshKeyProvider
{
    public IReadOnlyList<SshPublicIdentityResponce> ListIdentities() =>
        accountSessionManager.ActiveSession is null ? [] : vaultService.GetAvailableSshKeys();

    public async ValueTask<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token)
    {
        if (accountSessionManager.ActiveSession is null || vaultService.GetSsh(request.PublicKeyBlob) is not { } cipher)
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
