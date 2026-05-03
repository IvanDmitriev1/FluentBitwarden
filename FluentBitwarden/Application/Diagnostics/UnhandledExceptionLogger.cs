using System.Diagnostics;
using System.Text;
using FluentBitwarden.Shared.Extensions;

namespace FluentBitwarden.Application.Diagnostics;

public static class UnhandledExceptionLogger
{
    public static void WriteException(Exception e)
    {
        Debug.WriteLine("Unhandled exception! {0}", e);

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string logFilePath = Path.Combine(localAppData, "FluentBitwarden", "Logs", "unhandled-exceptions.log");
        string? logDirectory = Path.GetDirectoryName(logFilePath);

        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        File.AppendAllText(logFilePath, BuildUnhandledExceptionLogEntry(e), Encoding.UTF8);
    }

    private static string BuildUnhandledExceptionLogEntry(Exception exception)
    {
        StringBuilder builder = new();


        builder.AppendLine(new string('-', 80));
        builder.Append("Timestamp: ").AppendLine(DateTimeOffset.Now.ToString("O"));
        builder.AppendLine("Source: Application.UnhandledException");
        builder.Append("Packaged: ").AppendLine(PackageHelper.IsPackaged.ToString());
        builder.Append("ProcessId: ").AppendLine(Environment.ProcessId.ToString());
        builder.Append("BaseDirectory: ").AppendLine(AppContext.BaseDirectory);

        builder.Append("ExceptionType: ").AppendLine(exception.GetType().FullName ?? exception.GetType().Name);
        builder.Append("ExceptionMessage: ").AppendLine(exception.Message);
        builder.AppendLine();
        builder.AppendLine(exception.ToString());

        builder.AppendLine();
        return builder.ToString();
    }
}