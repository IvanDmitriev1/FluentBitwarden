using FluentBitwarden.Infrastructure.Navigation.Lifecycle;

namespace FluentBitwarden.Infrastructure.Navigation;

public interface INavigationService
{
    bool CanGoBack { get; }

    void NavigateTo<T>(IPageNavigationParameter? parameter = null) where T : Page;

    bool GoBack();
}
