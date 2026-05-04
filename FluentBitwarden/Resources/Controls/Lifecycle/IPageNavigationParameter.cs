namespace FluentBitwarden.Resources.Controls.Lifecycle;

public interface IPageNavigationParameter
{
    Task Load(object dataContext, CancellationToken cancellationToken);
}