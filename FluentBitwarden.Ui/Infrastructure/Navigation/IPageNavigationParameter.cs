namespace FluentBitwarden.Infrastructure.Navigation;

public interface IPageNavigationParameter
{
    Task LoadAsync(object dataContext, CancellationToken cancellationToken);
}