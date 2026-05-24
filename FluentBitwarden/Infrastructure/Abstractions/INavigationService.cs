using FluentBitwarden.UI.Controls.Lifecycle;

namespace FluentBitwarden.Infrastructure.Abstractions;

public interface INavigationService
{
    bool CanGoBack { get; }

    void NavigateTo<T>(IPageNavigationParameter? parameter = null) where T : Page;

    bool GoBack();
}
