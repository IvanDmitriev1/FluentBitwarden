using System.Diagnostics;
using FluentBitwarden.Ui.Abstractions;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Services;

public sealed class FrameNavigationService : INavigationService
{
    private WeakReference<Frame>? _frame;

    public bool CanGoBack => _frame?.TryGetTarget(out var frame) == true && frame.CanGoBack;

    public void Initialize(Frame frame)
    {
        if (_frame is not null)
        {
            _frame.SetTarget(frame);
        }
        else
        {
            _frame = new WeakReference<Frame>(frame);
        }
    }

    public void Navigate<T>(object? parameter = null, bool clearBackStack = false) where T : Page
    {
        if (_frame is null || !_frame.TryGetTarget(out var frame))
        {
            Debug.Fail("Frame reference is not set or has been collected.");
            return;
        }

        if (frame.Content is T)
            return;

        var navigated = frame.Navigate(typeof(T), parameter);

        if (navigated && clearBackStack)
        {
            frame.BackStack.Clear();
        }

        Debug.Assert(navigated, "Navigation failed.");
    }

    public bool GoBack()
    {
        if (_frame is null || !_frame.TryGetTarget(out var frame) || !frame.CanGoBack)
        {
            return false;
        }

        frame.GoBack();
        return true;
    }
}
