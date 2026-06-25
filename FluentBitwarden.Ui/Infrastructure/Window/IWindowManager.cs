namespace FluentBitwarden.Infrastructure.Window;

public interface IWindowManager : IThemeChangeable
{
    event EventHandler<IWindowManager, WindowMode>? WindowClosed;
    WindowMode ActiveMode { get; }
    IntPtr WindowHandle { get; }

    void ShowOrCreateWindow(WindowMode mode);
    void ReplaceWindow(WindowMode mode);
    void ActivateWindow();
    void CloseWindow();

    void ReplacePage<TPage>(IPageNavigationParameter? parameter = null) where TPage : Page;
    Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog, CancellationToken cancellationToken = default);
}
