using FluentBitwarden.Modules.SshAgent.Models;

namespace FluentBitwarden.Modules.SshAgent.Abstractions;

internal interface ISshKeyProvider
{
    IReadOnlyList<SshPublicIdentity> ListIdentities();
    SshSignatureResult SignAsync(SshSignRequest request);
}