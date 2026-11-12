namespace FluentBitwarden.Ui.Screens.Setup;

[DependencyProperty<string>("Email", DefaultValue = "")]
[DependencyProperty<string>("ServerDisplayName", DefaultValue = "")]
public sealed partial class SetupAccountSummary : UserControl
{
    public SetupAccountSummary() => InitializeComponent();
}
