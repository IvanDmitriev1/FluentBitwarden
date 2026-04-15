namespace FluentBitwarden.Modules.AppState.Abstractions;

internal interface IAppFirstRunService
{
    Task InitializeAsync();
}
