namespace FluentBitwarden.Views.Accounts.SignIn;

[DependencyProperty<string>("Email")]
[DependencyProperty<string>("ServerDisplayName")]
public sealed partial class LoginAccountSummary : UserControl
{
    public LoginAccountSummary()
    {
        InitializeComponent();
    }
}