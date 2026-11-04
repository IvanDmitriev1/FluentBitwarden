namespace FluentBitwarden.Infrastracture.Hosting.Abstractions;

public interface IWinUiHost
{
    IServiceProvider Services { get; }

    void Run();
}
