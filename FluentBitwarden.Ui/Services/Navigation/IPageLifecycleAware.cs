namespace FluentBitwarden.Services.Navigation;

public interface IPageLifecycleAwareBase
{
    /// <summary>
    /// Runs when a page is being unloaded.
    /// </summary>
    void OnUnloading();
}

public interface IPageLifecycleAware : IPageLifecycleAwareBase
{
    Task OnLoadingAsync(CancellationToken cancellationToken);
}

public interface IPageLifecycleAware<in TParam> : IPageLifecycleAwareBase where TParam : class
{
    Task OnLoadingAsync(TParam param, CancellationToken cancellationToken);
}
