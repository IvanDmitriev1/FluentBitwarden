namespace FluentBitwarden.Platform.Infrastructure.ProcessManager;

public interface IProcessManager
{
    bool IsRunning { get; }

    event Action ProcessExited;

    void Activate();
    void Exit();
}
