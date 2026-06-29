using Windows.ApplicationModel;

namespace FluentBitwarden.Platform.Infrastructure.ProcessManager;

public abstract class ProcessManager : IProcessManager
{
    protected ProcessManager(string exeName, string exePath)
    {
        string packageRoot = Package.Current.InstalledLocation.Path;

        _executablePath = Path.Combine(packageRoot, exePath, exeName);
        _exeWorkingDirectory = Path.GetDirectoryName(_executablePath) ?? throw new InvalidOperationException();
    }

    private readonly string _executablePath;
    private readonly string _exeWorkingDirectory;

    private readonly Lock _lock = new();
    private Process? _uiProcess;

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _uiProcess is not null;
            }
        }
    }

    public event Action? ProcessExited;

    public virtual void Activate()
    {
        LunchProcess(string.Empty);
    }

    public void Exit()
    {
        using var _ =  _lock.EnterScope();
        bool isProcessRunning = _uiProcess is not null;

        if (isProcessRunning)
        {
            _uiProcess?.CloseMainWindow();
        }

        CleanUpUiProcess();
    }

    protected void LunchProcess(string args)
    {
        using var _ = _lock.EnterScope();

        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            Arguments = args,
            WorkingDirectory = _exeWorkingDirectory,
            UseShellExecute = false
        };

        var process = Process.Start(startInfo);
        ArgumentNullException.ThrowIfNull(process);

        if (_uiProcess is not null)
        {
            process.Dispose();
            return;
        }


        process.EnableRaisingEvents = true;
        _uiProcess = process;
        _uiProcess.Exited += ProcessOnExitedHandler;
    }

    protected virtual void OnProcessExited()
    {
        ProcessExited?.Invoke();
    }

    private void ProcessOnExitedHandler(object? sender, EventArgs e)
    {
        CleanUpUiProcess();
        OnProcessExited();
    }
    private void CleanUpUiProcess()
    {
        using var _ = _lock.EnterScope();

        if (_uiProcess is null)
            return;

        _uiProcess.Exited -= ProcessOnExitedHandler;
        _uiProcess.Dispose();
        _uiProcess = null;
    }
}