using FluentBitwarden.AppHost.Modules.SshAgent.Models;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Abstractions;

internal interface ISshKeyProvider
{
    Task<SshIdentityQueryResult> ListIdentitiesAsync(CancellationToken token);
    Task<SshSignatureResult> SignAsync(SshSignRequest request, CancellationToken token);
}
