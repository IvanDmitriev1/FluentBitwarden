using FluentBitwarden.Modules.SshAgent.Models;

namespace FluentBitwarden.Modules.SshAgent.Abstractions;

internal interface ISshKeyProvider
{
    IReadOnlyList<SshPublicIdentityResponce> ListIdentities();
    ValueTask<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token);
}