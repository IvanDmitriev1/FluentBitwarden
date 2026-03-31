namespace FluentBitwarden.Shared.Behaviors.Lifecycle;

public interface IPageNavigationParameter
{
    Task Load(object dataContext, CancellationToken cancellationToken);
}