namespace FluentBitwarden.Shared.Navigation.Lifecycle;

public interface IPageNavigationParameter
{
    Task Load(object dataContext, CancellationToken cancellationToken);
}