using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.Email;

[DependencyProperty<SetupEmailViewModel>("ViewModel")]
public sealed partial class SetupEmailView : UserControl
{
    public SetupEmailView() => InitializeComponent();
}
