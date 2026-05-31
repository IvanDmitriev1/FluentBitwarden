namespace FluentBitwarden.Views.Accounts.LogIn;

[DependencyProperty<string>("Email")]
[DependencyProperty<string>("ServerDisplayName")]
public sealed partial class LoginAccountSummary : UserControl
{
    public LoginAccountSummary()
    {
        InitializeComponent();
    }
}