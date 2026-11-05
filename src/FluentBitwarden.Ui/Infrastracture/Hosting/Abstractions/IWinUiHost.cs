namespace FluentBitwarden.Ui.Infrastracture.Hosting.Abstractions;

public interface IWinUiHost
{
    IServiceProvider Services { get; }

    void Run();
}
