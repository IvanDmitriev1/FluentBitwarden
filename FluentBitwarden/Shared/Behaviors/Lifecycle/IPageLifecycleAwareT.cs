using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FluentBitwarden.Shared.Behaviors.Lifecycle;

public interface IPageLifecycleAwareParam
{
    /// <summary>
    /// Runs when a page is being loaded.
    /// </summary>
    Task OnLoadingAsync(object? parameter, CancellationToken cancellationToken);

    /// <summary>
    /// Runs when a page is being unloaded.
    /// </summary>
    void OnUnloading();
}


/// <summary>
/// Defines async lifecycle hooks for pages that participate in navigation.
/// </summary>
public interface IPageLifecycleAwareParam<in TParam> : IPageLifecycleAwareParam where TParam : class
{
    /// <summary>
    /// Runs when a page is being loaded.
    /// </summary>
    Task OnLoadingAsync(TParam param, CancellationToken cancellationToken);

    Task IPageLifecycleAwareParam.OnLoadingAsync(object? parameter, CancellationToken cancellationToken)
    {
        Debug.Assert(parameter is TParam);
        return OnLoadingAsync(Unsafe.As<TParam>(parameter), cancellationToken);
    }
}