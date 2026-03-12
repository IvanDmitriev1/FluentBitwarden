using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Abstractions;

/// <summary>
/// Coordinates navigation between application pages.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Initializes navigation with the root frame.
    /// </summary>
    void Initialize(Frame frame);

    /// <summary>
    /// Navigates to a page and optionally clears navigation history.
    /// </summary>
    void Navigate<T>(object? parameter = null, bool clearBackStack = false) where T : Page;

    /// <summary>
    /// Indicates whether navigation can move back to a previous page.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Navigates back to the previous page when possible.
    /// </summary>
    bool GoBack();
}
