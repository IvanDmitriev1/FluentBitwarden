using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.Password;

[DependencyProperty<SetupPasswordViewModel>("ViewModel")]
public sealed partial class SetupPasswordView : UserControl
{
    public SetupPasswordView() => InitializeComponent();
}
