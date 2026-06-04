namespace FluentBitwarden.Shared.Navigation.Lifecycle;

public interface IPageNavigationParameter
{
    Task LoadAsync(object dataContext, CancellationToken cancellationToken);
}