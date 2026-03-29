namespace FluentBitwarden.Shell.Navigation;

public interface INavigationService
{
    bool CanGoBack { get; }

    void NavigateTo<T>() where T : Page;

    void NavigateTo<TPage, TMessage>(TMessage message)
        where TPage : Page
        where TMessage : class;

    bool GoBack();
}
