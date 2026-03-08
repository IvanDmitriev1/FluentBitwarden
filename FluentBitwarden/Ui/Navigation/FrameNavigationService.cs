using System.Diagnostics;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui.Controls;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Navigation;

public sealed class FrameNavigationService : INavigationService
{
    private Frame? _frame;
    public bool CanGoBack => _frame?.CanGoBack == true;

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public void Navigate<T>(object? parameter = null, bool clearBackStack = false) where T : CorePage
    {
        ArgumentNullException.ThrowIfNull(_frame);
        var navigated = _frame.Navigate(typeof(T), parameter);

        if (navigated && clearBackStack)
        {
            _frame.BackStack.Clear();
        }

        Debug.Assert(navigated, "Navigation failed.");
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
