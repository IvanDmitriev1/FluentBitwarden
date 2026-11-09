namespace FluentBitwarden.Ui.Screens.Loading;

[DependencyProperty<LoadingViewModel>("ViewModel")]
public sealed partial class LoadingView : UserControl
{
    public LoadingView() => InitializeComponent();
}
