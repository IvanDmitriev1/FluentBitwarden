using FluentBitwarden.Ui.Dialogs.Abstractions;
using FluentBitwarden.Contracts.Modules.Ssh;

namespace FluentBitwarden.Ui.Dialogs.SshKeyApproval;

public sealed partial class SshKeyApprovalDialog : ContentDialog,
    IContentDialog<SshKeyApprovalDialog, SshUserActionRequest, SshKeyApprovalResult>
{
    public static SshKeyApprovalDialog Create(SshUserActionRequest request) => new(new SshKeyApprovalViewModel(request));

    public SshKeyApprovalDialog(SshKeyApprovalViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
    }

    public SshKeyApprovalViewModel ViewModel { get; }
    public SshKeyApprovalResult Result => ViewModel.Result;
}
