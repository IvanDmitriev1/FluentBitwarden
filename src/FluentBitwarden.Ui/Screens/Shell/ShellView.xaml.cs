using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Screens.Shell;

[DependencyProperty<ShellViewModel>("ViewModel")]
public sealed partial class ShellView : UserControl
{
    public ShellView() => InitializeComponent();
}
