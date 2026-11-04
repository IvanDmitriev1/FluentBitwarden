using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Infrastructure.UserDialogs.Abstractions;

namespace FluentBitwarden.Views.UserDialogs;

public sealed partial class SshUserActionDialog : ContentDialog, IUserDialog<SshUserActionResponse>
{
    public SshUserActionDialog(SshUserActionRequest request)
    {
        Request = request;
        InitializeComponent();
    }

    public SshUserActionRequest Request { get; }

    private SshUserActionResponse? _result;

    public bool TryGetResult(out SshUserActionResponse result)
    {
        result = _result.GetValueOrDefault();
        return _result.HasValue;
    }

    private void ApproveButton_Click(
        ContentDialog sender,
        ContentDialogButtonClickEventArgs args)
    {
        _result = new SshUserActionResponse(Approved: true);
        Hide();
    }

    private void DenyButton_Click(
        ContentDialog sender,
        ContentDialogButtonClickEventArgs args)
    {
        _result = new SshUserActionResponse(Approved: false);
        Hide();
    }
}
