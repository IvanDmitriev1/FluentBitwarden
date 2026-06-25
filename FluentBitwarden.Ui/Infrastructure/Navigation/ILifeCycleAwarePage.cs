namespace FluentBitwarden.Infrastructure.Navigation;

internal interface ILifeCycleAwarePage
{
    void Reload(IPageNavigationParameter? parameter);
}