namespace FluentBitwarden.Resources.Dialogs.Models;

internal sealed record SshUserActionDialogViewModel(
    string KeyName,
    string KeyFingerprint,
    bool IsForwarded);
