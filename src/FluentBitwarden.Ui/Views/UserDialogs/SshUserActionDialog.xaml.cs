using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Infrastructure.UserDialogs.Abstractions;

namespace FluentBitwarden.Views.UserDialogs;

public sealed partial class SshUserActionDialog : ContentDialog, IUserDialog<UserActionDialogOutcome>
{
    public SshUserActionDialog(SshUserActionRequest request)
    {
        Request = request;
        InitializeComponent();
    }

    private UserActionDialogOutcome? _result;

    public SshUserActionRequest Request { get; }

    public bool TryGetResult([MaybeNullWhen(false)] out UserActionDialogOutcome result)
    {
        if (_result is { } dialogResult)
        {
            result = dialogResult;
            return true;
        }

        result = default;
        return false;
    }

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
