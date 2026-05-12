namespace FluentBitwarden.UI.Controls.Lifecycle;

public interface IPageNavigationParameter
{
    Task Load(object dataContext, CancellationToken cancellationToken);
}