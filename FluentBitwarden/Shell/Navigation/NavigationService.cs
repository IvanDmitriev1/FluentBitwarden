using System.Diagnostics;

namespace FluentBitwarden.Shell.Navigation;

public sealed class NavigationService : INavigationService
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

    public void NavigateTo<T>() where T : Page
    {
        if (_frame is null || !_frame.TryGetTarget(out var frame))
            return;

        if (frame.Content is T)
            return;

        var navigated = frame.Navigate(typeof(T));

        frame.BackStack.Clear();
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