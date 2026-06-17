namespace FluentBitwarden.Infrastructure.Navigation.Lifecycle;

public interface IPageNavigationParameter
{
    Task LoadAsync(object dataContext, CancellationToken cancellationToken);
}