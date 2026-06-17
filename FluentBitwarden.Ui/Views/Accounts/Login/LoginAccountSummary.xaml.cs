namespace FluentBitwarden.Views.Accounts.Login;

[DependencyProperty<string>("Email")]
[DependencyProperty<string>("ServerDisplayName")]
public sealed partial class LoginAccountSummary : UserControl
{
    public LoginAccountSummary()
    {
        InitializeComponent();
    }
}