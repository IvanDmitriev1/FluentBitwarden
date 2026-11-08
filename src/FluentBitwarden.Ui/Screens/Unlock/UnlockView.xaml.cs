using DependencyPropertyGenerator;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Screens.Unlock;

[DependencyProperty<UnlockViewModel>("ViewModel")]
public sealed partial class UnlockView : UserControl
{
    public UnlockView() => InitializeComponent();
}
