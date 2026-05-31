using FluentBitwarden.AppHost.Modules.SshAgent.Models;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Abstractions;

internal interface ISshKeyProvider
{
    IReadOnlyList<SshPublicIdentityResponce> ListIdentities();
    ValueTask<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token);
}