using FluentBitwarden.Shared.Services.Abstractions.Dialog;

namespace FluentBitwarden.Resources.Dialogs.Models;

internal sealed record SshUserActionRequestViewModel(
    string KeyName,
    string KeyFingerprint,
    bool IsForwarded) : IContentDialogViewModel
{
    public string DataTemplateKey => "SshUserActionRequestViewModelTemplateKey"; 
    public string DialogTitle => "Approve SSH request?";
}