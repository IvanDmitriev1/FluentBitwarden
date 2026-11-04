using FluentBitwarden.Windows;
using WinUIEx;

namespace FluentBitwarden;

public partial class App
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        new MainWindow().Show();
    }
}
