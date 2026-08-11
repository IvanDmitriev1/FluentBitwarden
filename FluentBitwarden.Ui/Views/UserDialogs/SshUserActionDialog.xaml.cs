using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Infrastructure.UserDialogs;

namespace FluentBitwarden.Views.UserDialogs;

public sealed partial class SshUserActionDialog : ContentDialog, IUserDialog<UserActionDialogOutcome>
{
    public SshUserActionDialog(SshUserActionRequest request)
    {
        Request = request;
        InitializeComponent();
    }

    private UserActionDialogOutcome? _result;


    public UserActionDialogOutcome Result =>
        _result ?? throw new InvalidOperationException("The dialog was closed without a result.");

    public SshUserActionRequest Request { get; }

    private void ApproveButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SetResult(UserActionDialogOutcome.Approved);
    }

    private void DenyButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SetResult(UserActionDialogOutcome.Denied);
    }

    private void SetResult(UserActionDialogOutcome result)
    {
        _result = result;
        Hide();
    }
}
