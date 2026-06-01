namespace FluentBitwarden.AppHost.Modules.SshAgent.Models;

internal readonly record struct SshIdentityQueryResult(
    IReadOnlyList<SshPublicIdentityResponce> Identities,
    bool IsDenied)
{
    public static SshIdentityQueryResult Denied { get; } = new([], true);

    public static SshIdentityQueryResult Success(IReadOnlyList<SshPublicIdentityResponce> identities) =>
        new(identities, false);
}
