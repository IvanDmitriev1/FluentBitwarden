namespace FluentBitwarden.Application.Lifetime;

public interface IAppRestartService
{
    Task RestartForLockAsync();
}