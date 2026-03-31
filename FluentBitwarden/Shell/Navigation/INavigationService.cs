using FluentBitwarden.Shared.Behaviors.Lifecycle;

namespace FluentBitwarden.Shell.Navigation;

public interface INavigationService
{
    bool CanGoBack { get; }

    void NavigateTo<T>(IPageNavigationParameter? parameter = null) where T : Page;

    bool GoBack();
}
