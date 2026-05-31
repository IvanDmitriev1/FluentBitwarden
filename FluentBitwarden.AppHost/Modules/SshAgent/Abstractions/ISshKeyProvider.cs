using FluentBitwarden.AppHost.Modules.SshAgent.Models;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Abstractions;

internal interface ISshKeyProvider
{
    IReadOnlyList<SshPublicIdentityResponce> ListIdentities();
    Task<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token);
}