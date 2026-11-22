using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Ssh;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentBitwarden.Ui.Dialogs.SshKeyApproval;

public sealed partial class SshKeyApprovalViewModel(SshUserActionRequest request)
{
    private static readonly ResourceLoader ResourceLoader = new();

    public string KeyName { get; } = request.KeyName;
    public string? Destination { get; } = request.Destination;
    public bool IsForwarded { get; } = request.IsForwarded;
    public SshKeyApprovalResult Result { get; private set; } = SshKeyApprovalResult.Denied;

    [RelayCommand]
    private void PrimaryButton() => Result = SshKeyApprovalResult.Approved;
}
