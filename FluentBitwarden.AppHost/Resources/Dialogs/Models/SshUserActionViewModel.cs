namespace FluentBitwarden.Resources.Dialogs.Models;

public sealed record SshUserActionRequestViewModel(
    string KeyName,
    string KeyFingerprint,
    bool IsForwarded);
