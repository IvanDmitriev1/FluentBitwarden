using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Shell.Navigation;

public interface INavigationService
{
    bool CanGoBack { get; }

    void NavigateTo<T>() where T : Page;
    bool GoBack();
}