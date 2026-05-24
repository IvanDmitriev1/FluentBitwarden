using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.UI.Controls.Lifecycle;

namespace FluentBitwarden.Infrastructure.Implementations;

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

    public void NavigateTo<T>(IPageNavigationParameter? parameter = null) where T : Page
    {
        if (!TryGetFrame(out var frame))
            return;

        if (frame.Content is T)
            return;

        var navigated = frame.Navigate(typeof(T), parameter);

        frame.BackStack.Clear();
        Debug.Assert(navigated, "Navigation failed.");
    }

    public bool GoBack()
    {
        if (!TryGetFrame(out var frame) || !frame.CanGoBack)
        {
            return false;
        }

        frame.GoBack();
        return true;
    }

    private bool TryGetFrame([NotNullWhen(true)] out Frame? frame)
    {
        frame = null;
        return _frame?.TryGetTarget(out frame) == true;
    }
}
