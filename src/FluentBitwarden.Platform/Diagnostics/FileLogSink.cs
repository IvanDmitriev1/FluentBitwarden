using System.Text;

namespace FluentBitwarden.Platform.Diagnostics;

internal sealed class FileLogSink : IDisposable
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly Lock _lock = new();
    private readonly string _filePath;
    private readonly string _rolledFilePath;

    private StreamWriter? _writer;
    private bool _disposed;

    public FileLogSink(string logName)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string directory = Path.Combine(localAppData, "FluentBitwarden", "Logs");

        _filePath = Path.Combine(directory, $"{logName}.log");
        _rolledFilePath = Path.Combine(directory, $"{logName}.1.log");
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "A failing sink must never surface as an exception at the log call site; a lost log entry is preferable to breaking the caller.")]
    public void Write(string entry)
    {
        using var _ = _lock.EnterScope();

        if (_disposed)
        {
            return;
        }

        try
        {
            StreamWriter writer = _writer ??= OpenWriter();

            if (writer.BaseStream.Length >= MaxFileSizeBytes)
            {
                writer = Roll();
            }

            writer.Write(entry);
        }
        catch (Exception)
        {
            // The sink is best-effort. Drop the writer so the next write retries from a clean state.
            _writer?.Dispose();
            _writer = null;
        }
    }

    public void Dispose()
    {
        using var _ = _lock.EnterScope();

        _disposed = true;
        _writer?.Dispose();
        _writer = null;
    }

    private StreamWriter Roll()
    {
        _writer?.Dispose();
        _writer = null;

        File.Move(_filePath, _rolledFilePath, overwrite: true);

        return _writer = OpenWriter();
    }

    private StreamWriter OpenWriter()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        return new StreamWriter(_filePath, append: true, Encoding.UTF8) { AutoFlush = true };
    }
}
