namespace FluentBitwarden.Shell.Navigation;

public interface INavigationService
{
    bool CanGoBack { get; }

    void NavigateTo<T>(object? param = null) where T : Page;

    bool GoBack();
}
