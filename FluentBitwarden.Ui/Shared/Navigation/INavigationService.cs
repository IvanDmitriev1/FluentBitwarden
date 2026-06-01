using FluentBitwarden.Shared.Navigation.Lifecycle;

namespace FluentBitwarden.Shared.Navigation;

public interface INavigationService
{
    bool CanGoBack { get; }

    void NavigateTo<T>(IPageNavigationParameter? parameter = null) where T : Page;

    bool GoBack();
}
