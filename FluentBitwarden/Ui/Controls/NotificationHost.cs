using System.Diagnostics;
using CommunityToolkit.WinUI;
using DependencyPropertyGenerator;
using FluentBitwarden.Ui.Abstractions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace FluentBitwarden.Ui.Controls;

[TemplatePart(Name = InfoBarPartName, Type = typeof(InfoBar))]
[DependencyProperty<string>("NotificationTitle", DefaultValue = "")]
[DependencyProperty<string>("NotificationMessage", DefaultValue = "")]
[DependencyProperty<InfoBarSeverity>("NotificationSeverity", DefaultValue = InfoBarSeverity.Informational)]
[DependencyProperty<bool>("IsOpen", DefaultValue = false)]
public sealed partial class NotificationHost : Control
{
    private const string InfoBarPartName = "PART_InfoBar";
    private readonly Queue<NotificationMessage> _queue = new();

    private DispatcherQueueTimer? _autoCloseTimer;
    private NotificationMessage? _current;
    private InfoBar? _infoBar;

    public NotificationHost()
    {
        DefaultStyleKey = typeof(NotificationHost);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override void OnApplyTemplate()
    {
        DetachInfoBarEvents();

        base.OnApplyTemplate();

        _infoBar = GetTemplateChild(InfoBarPartName) as InfoBar;
        AttachInfoBarEvents();

        if (_current is null && !IsOpen)
        {
            ShowNext();
        }
    }

    public void QueueNotification(NotificationMessage notification)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _queue.Enqueue(notification);

            if (_current is null && !IsOpen)
            {
                ShowNext();
            }
        });
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureAutoCloseTimer();

        if (_current is not null && IsOpen)
        {
            ScheduleAutoClose(_current);
        }
        else if (_current is null && !IsOpen)
        {
            ShowNext();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer?.Stop();
        _autoCloseTimer = null;
    }

    private void ShowNext()
    {
        if (_current is not null || _queue.Count == 0)
        {
            return;
        }

        _current = _queue.Dequeue();

        NotificationTitle = _current.Title;
        NotificationMessage = _current.Message;
        NotificationSeverity = _current.Severity;
        IsOpen = true;

        ScheduleAutoClose(_current);
    }

    private void ScheduleAutoClose(NotificationMessage notification)
    {
        if (notification.Duration <= TimeSpan.FromSeconds(5))
        {
            return;
        }

        DispatcherQueueTimer autoCloseTimer = EnsureAutoCloseTimer();
        Debug.Assert(_current is not null);

        autoCloseTimer.Stop();
        autoCloseTimer.Debounce(
            action: () =>
            {
                if (_current == notification)
                {
                    IsOpen = false;
                }
            },
            interval: notification.Duration,
            immediate: false);
    }

    private DispatcherQueueTimer EnsureAutoCloseTimer()
    {
        if (_autoCloseTimer is not null)
        {
            return _autoCloseTimer;
        }

        var dispatcherQueue = DispatcherQueue
                              ?? DispatcherQueue.GetForCurrentThread()
                              ?? throw new InvalidOperationException("No dispatcher queue is available for notifications.");

        _autoCloseTimer = dispatcherQueue.CreateTimer();
        return _autoCloseTimer;
    }

    private void AttachInfoBarEvents()
    {
        if (_infoBar is null)
        {
            return;
        }

        _infoBar.Closed += OnInfoBarClosed;
        _infoBar.PointerEntered += OnInfoBarPointerEntered;
        _infoBar.PointerExited += OnInfoBarPointerExited;
    }

    private void DetachInfoBarEvents()
    {
        if (_infoBar is null)
        {
            return;
        }

        _infoBar.Closed -= OnInfoBarClosed;
        _infoBar.PointerEntered -= OnInfoBarPointerEntered;
        _infoBar.PointerExited -= OnInfoBarPointerExited;
        _infoBar = null;
    }

    private void OnInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        _autoCloseTimer?.Stop();
        IsOpen = false;
        NotificationTitle = string.Empty;
        NotificationMessage = string.Empty;
        NotificationSeverity = InfoBarSeverity.Informational;
        _current = null;

        ShowNext();
    }

    private void OnInfoBarPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _autoCloseTimer?.Stop();
    }

    private void OnInfoBarPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _autoCloseTimer?.Start();
    }
}
