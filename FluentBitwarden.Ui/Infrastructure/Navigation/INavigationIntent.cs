namespace FluentBitwarden.Infrastructure.Navigation;

public interface INavigationIntent
{
    IPageNavigationParameter CreateParameter();
}