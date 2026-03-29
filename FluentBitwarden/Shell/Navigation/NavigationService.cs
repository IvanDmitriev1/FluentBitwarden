using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Messaging;

namespace FluentBitwarden.Shell.Navigation;

public sealed class NavigationService(IMessenger messenger) : INavigationService
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
        if (!TryGetFrame(out var frame))
            return;

        if (frame.Content is T)
            return;

        var navigated = frame.Navigate(typeof(T));

        frame.BackStack.Clear();
        Debug.Assert(navigated, "Navigation failed.");
    }

    public void NavigateTo<TPage, TMessage>(TMessage message)
        where TPage : Page
        where TMessage : class
    {
        if (!TryGetFrame(out var frame))
            return;

        if (frame.Content is TPage)
        {
            messenger.Send(message);
            return;
        }

        var navigated = frame.Navigate(typeof(TPage));
        Debug.Assert(navigated, "Navigation failed.");

        if (!navigated)
            return;

        frame.BackStack.Clear();
        messenger.Send(message);
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
