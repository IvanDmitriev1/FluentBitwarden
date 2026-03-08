using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Abstractions;

public interface INavigationService
{
    void Initialize(Frame frame);

    bool Navigate(Type pageType, object? parameter = null, bool clearBackStack = false);

    bool CanGoBack { get; }

    bool GoBack();
}
