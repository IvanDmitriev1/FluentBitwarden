namespace FluentBitwarden.AppHost.Modules.SshAgent.Models;

public sealed record SshUserActionRequestViewModel(
    string KeyName,
    string KeyFingerprint,
    bool IsForwarded);
