namespace FluentBitwarden.Infrastructure.Navigation.Lifecycle;

public sealed class PageNavigationParameter<TParam>(TParam value) : IPageNavigationParameter
    where TParam : class
{
    public TParam Value { get; } = value;

    public Task LoadAsync(object dataContext, CancellationToken cancellationToken) => dataContext switch
    {
        IPageLifecycleAware<TParam> aware => aware.OnLoadingAsync(Value, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(dataContext), dataContext, null)
    };
}

public static class PageNavigationParameter
{
    public static PageNavigationParameter<TParam> From<TParam>(TParam value) where TParam : class => new(value);
}