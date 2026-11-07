namespace FluentBitwarden.Ui.Infrastructure.Hosting.Abstractions;

public interface IWinUiHost
{
    IServiceProvider Services { get; }

    void Run();
}
