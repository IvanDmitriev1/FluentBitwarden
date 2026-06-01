using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Platform.Windowing;

[AttachedDependencyProperty<DependencyObject>("OwnerElement")]
[AttachedDependencyProperty<Frame>("NavigationFrame")]
[AttachedDependencyProperty<UIElement>("Content")]
[AttachedDependencyProperty<ICommand>("PaneToggleCommand")]
public static partial class TitlebarProperties
{
    [field:MaybeNull]
    private static TitleBar TargetTitleBar
    {
        get => field ?? throw new InvalidOperationException("TargetTitleBar is not set");
        set;
    }

    public static void SetTargetTitleBar(TitleBar newValue)
    {
        TargetTitleBar = newValue;
        TargetTitleBar.PaneToggleRequested += TitlebarOnPaneToggleRequested;
        TargetTitleBar.BackRequested += TitlebarOnBackRequested;
    }

    static partial void OnNavigationFrameChanged(DependencyObject dependencyObject, Frame? oldValue, Frame? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.Navigated -= NavigationFrameOnNavigated;
            SetOwnerElement(oldValue, null);
            SetOwnerElement(TargetTitleBar, null);
        }

        if (newValue is null)
            throw new InvalidOperationException("NavigationFrame cannot be null");

        newValue.Navigated += NavigationFrameOnNavigated;
        SetOwnerElement(newValue, dependencyObject);
        SetOwnerElement(TargetTitleBar, dependencyObject);

        ApplyState(dependencyObject);
    }

    private static void NavigationFrameOnNavigated(object sender, NavigationEventArgs e)
    {
        Frame frame = (Frame)sender;
        if (GetOwnerElement(frame) is not { } ownerElement)
            return;

        ApplyState(ownerElement);
    }

    private static void TitlebarOnPaneToggleRequested(TitleBar sender, object args)
    {
        if (GetOwnerElement(sender) is not { } ownerElement)
            return;

        ICommand? command = GetPaneToggleCommand(ownerElement);
        if (command?.CanExecute(null) is true)
        {
            command.Execute(null);
        }
    }

    private static void TitlebarOnBackRequested(TitleBar sender, object args)
    {
        if (GetOwnerElement(sender) is not { } ownerElement)
            return;

        Frame? navigationFrame = GetNavigationFrame(ownerElement);
        if (navigationFrame?.CanGoBack is true)
        {
            navigationFrame.GoBack();
            ApplyState(ownerElement);
        }
    }

    private static void ApplyState(DependencyObject sender)
    {
        var navigationFrame = GetNavigationFrame(sender);
        var paneToggleCommand = GetPaneToggleCommand(sender);

        TargetTitleBar.Content = GetContent(sender);
        TargetTitleBar.IsPaneToggleButtonVisible = paneToggleCommand?.CanExecute(null) is true;
        TargetTitleBar.IsBackButtonVisible = navigationFrame?.CanGoBack is true;

        if (TargetTitleBar.Content is FrameworkElement contentElement &&
            sender is FrameworkElement senderElement)
        {
            contentElement.DataContext = senderElement.DataContext;
        }
    }
}