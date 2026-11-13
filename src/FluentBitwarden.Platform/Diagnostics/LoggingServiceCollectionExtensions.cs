using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Settings.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        return services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.Services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(static provider =>
            {
                var settings = provider.GetRequiredService<ISettingsStore>();
                LogLevels levels = settings.Get(AppSettingKeys.Diagnostics.LogLevelsKey);
                return new ConfigureOptions<LoggerFilterOptions>(
                    options => options.AddFilter(level => LogLevelsExtensions.IsEnabled(levels, level)));
            });
            builder.Services.AddSingleton<ILoggerProvider>(_ => new FileLoggerProvider(logName));
        });
    }
}
