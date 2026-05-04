using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.SshAgent.Models;
using FluentBitwarden.Modules.SshAgent.Models.OpenSsh;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Shared.Services.Abstractions.Dialog;
using FluentBitwarden.Shared.Services.Implementations;
using System.Linq;
using FluentBitwarden.Resources.Dialogs.Models;

namespace FluentBitwarden.Modules.SshAgent.Services;

internal sealed class SshKeyProvider(
    ICurrentSessionAccessor sessionAccessor,
    IVaultSyncService vaultSyncService,
    IContentDialogService contentDialogService) : ISshKeyProvider
{
    public IReadOnlyList<SshPublicIdentityResponce> ListIdentities()
    {
        if (!sessionAccessor.IsAuthenticated)
            return [];

        return vaultSyncService.Ciphers.OfType<SshKeyCipher>()
            .Select(static c => new SshPublicIdentityResponce(c.PublicKey.KeyBlob, c.Name))
            .ToList();
    }

    public async ValueTask<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token)
    {
        if (!sessionAccessor.IsAuthenticated)
            return SshSignatureResult.Failed;

        var cipher = vaultSyncService.Ciphers.OfType<SshKeyCipher>()
            .FirstOrDefault(c => c.PublicKey.KeyBlob.SequenceEqual(request.PublicKeyBlob.Span));

        if (cipher is null)
            return SshSignatureResult.Failed;


        var userAction = await contentDialogService.ShowUserActionAsync(new SshUserActionRequestViewModel(
            KeyName: cipher.Name,
            KeyFingerprint: cipher.KeyFingerprint,
            IsForwarded: false
        ));

        if (userAction == UserActionDialogOutcome.Denied)
            return SshSignatureResult.Failed;

        var privateKey = OpenSshEd25519Key.Parse(cipher.PrivateKey.AsMemory());
        var signedData = privateKey.Sign(request.Data);

        return new SshSignatureResult(OpenSshEd25519Key.AlgorithmName, signedData);
    }
}