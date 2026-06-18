namespace FluentBitwarden.Services.Navigation;

public interface IPageNavigationParameter
{
    Task LoadAsync(object dataContext, CancellationToken cancellationToken);
}