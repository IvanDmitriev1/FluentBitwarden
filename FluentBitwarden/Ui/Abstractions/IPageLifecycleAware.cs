namespace FluentBitwarden.Ui.Abstractions;

/// <summary>
/// Defines async lifecycle hooks for pages that participate in navigation.
/// </summary>
public interface IPageLifecycleAware
{
    /// <summary>
    /// Runs when a page is being loaded.
    /// </summary>
    Task OnLoadingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs when a page is being unloaded.
    /// </summary>
    Task OnUnloadingAsync(CancellationToken cancellationToken);
}
