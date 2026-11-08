using DependencyPropertyGenerator;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Screens.Setup;

[DependencyProperty("ViewModel", typeof(SetupViewModel))]
public sealed partial class SetupView : UserControl
{
    public SetupView() => InitializeComponent();
}
