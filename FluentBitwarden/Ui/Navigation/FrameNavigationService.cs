using FluentBitwarden.Ui.Abstractions;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Navigation;

public sealed class FrameNavigationService : INavigationService
{
    private Frame? _frame;

    public bool CanGoBack => _frame?.CanGoBack == true;

    public void Initialize(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _frame = frame;
    }

    public bool Navigate(Type pageType, object? parameter = null, bool clearBackStack = false)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        if (!typeof(Page).IsAssignableFrom(pageType))
        {
            throw new ArgumentException("Navigation target must derive from Page.", nameof(pageType));
        }

        if (_frame is null)
        {
            throw new InvalidOperationException("Navigation frame has not been initialized.");
        }

        var navigated = _frame.Navigate(pageType, parameter);

        if (navigated && clearBackStack)
        {
            _frame.BackStack.Clear();
        }

        return navigated;
    }

    public bool GoBack()
    {
        if (_frame is null || !_frame.CanGoBack)
        {
            return false;
        }

        _frame.GoBack();
        return true;
    }
}
