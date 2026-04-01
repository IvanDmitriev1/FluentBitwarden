using FluentBitwarden.Shared.Behaviors.Lifecycle;

namespace FluentBitwarden.Views.Shell.Navigation;

public interface INavigationService
{
    bool CanGoBack { get; }

    void NavigateTo<T>(IPageNavigationParameter? parameter = null) where T : Page;

    bool GoBack();
}
