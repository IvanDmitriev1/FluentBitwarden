using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.SshAgent.Models;
using FluentBitwarden.Modules.SshAgent.Models.OpenSsh;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Resources.Dialogs.Models;
using FluentBitwarden.Infrastructure.Services.Abstractions.Dialog;
using FluentBitwarden.Modules.AppState.Models;

namespace FluentBitwarden.Modules.SshAgent.Services;

internal sealed class SshKeyProvider(
    IAccountSessionManager accountSessionManager,
    IVaultService vaultService,
    IContentDialogService contentDialogService) : ISshKeyProvider
{
    private static readonly ContentDialogOptions DialogOptions = new(
        Title: "Approve SSH request?",
        PrimaryButtonText: "Approve",
        SecondaryButtonText: "Deny",
        DefaultButton: ContentDialogButton.Secondary,
        DataTemplateKey: "SshUserActionRequestViewModelTemplateKey"
    );

    public IReadOnlyList<SshPublicIdentityResponce> ListIdentities() =>
        accountSessionManager.ActiveSession is null ? [] : vaultService.GetAvailableSshKeys();

    public async ValueTask<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token)
    {
        if (accountSessionManager.ActiveSession is null || vaultService.GetSsh(request.PublicKeyBlob) is not { } cipher)
            return SshSignatureResult.Failed;

        var userVerificationPolicy = SettingsStore.Instance.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey);
        if (userVerificationPolicy == SensitiveActionPolicy.RequireUserAction)
        {
            var userAction = await contentDialogService.ShowUserActionAsync(
                new SshUserActionRequestViewModel(
                    KeyName: cipher.Name,
                    KeyFingerprint: cipher.KeyFingerprint,
                    IsForwarded: false),
                DialogOptions);

            if (userAction == UserActionDialogOutcome.Denied)
                return SshSignatureResult.Failed;
        }

        var privateKey = OpenSshEd25519Key.Parse(cipher.PrivateKey.AsMemory());
        var signedData = privateKey.Sign(request.Data);

        return new SshSignatureResult(OpenSshEd25519Key.AlgorithmName, signedData);
    }
}