using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Contracts.Settings.Models;
using FluentBitwarden.Platform.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentBitwarden.Platform.Diagnostics;

public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the process-wide file log sink, filtered by the <see cref="LogLevels"/> setting as it
    /// reads at startup. A changed setting therefore takes effect on the next application start.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="logName">Names the process log file, e.g. "apphost" writes Logs\apphost.log.</param>
    public static IServiceCollection AddAppLogging(this IServiceCollection services, string logName)
    {
        LogLevels levels = SettingsStore.Instance.Get(AppSettingKeys.Diagnostics.LogLevelsKey);

        return services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddFilter(level => LogLevelsExtensions.IsEnabled(levels, level));
            builder.Services.AddSingleton<ILoggerProvider>(_ => new FileLoggerProvider(logName));
        });
    }
}
