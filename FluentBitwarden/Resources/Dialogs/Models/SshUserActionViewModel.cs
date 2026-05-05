namespace FluentBitwarden.Resources.Dialogs.Models;

internal sealed record SshUserActionRequestViewModel(
    string KeyName,
    string KeyFingerprint,
    bool IsForwarded);