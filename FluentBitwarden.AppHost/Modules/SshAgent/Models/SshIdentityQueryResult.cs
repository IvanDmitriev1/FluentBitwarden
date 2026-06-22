namespace FluentBitwarden.AppHost.Modules.SshAgent.Models;

internal readonly record struct SshIdentityQueryResult(IReadOnlyList<SshPublicIdentityResponce> Identities)
{
    public static SshIdentityQueryResult Success(IReadOnlyList<SshPublicIdentityResponce> identities) =>
        new(identities);
}
