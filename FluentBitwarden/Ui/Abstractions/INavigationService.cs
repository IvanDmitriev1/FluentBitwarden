using FluentBitwarden.Ui.Controls;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Abstractions;

public interface INavigationService
{
    void Initialize(Frame frame);

    void Navigate<T>(object? parameter = null, bool clearBackStack = false) where T : CorePage;

    bool CanGoBack { get; }

    bool GoBack();
}
