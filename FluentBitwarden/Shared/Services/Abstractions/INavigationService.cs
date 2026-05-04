using FluentBitwarden.Resources.Controls.Lifecycle;

namespace FluentBitwarden.Shared.Services.Abstractions;

public interface INavigationService
{
    bool CanGoBack { get; }

    void NavigateTo<T>(IPageNavigationParameter? parameter = null) where T : Page;

    bool GoBack();
}
