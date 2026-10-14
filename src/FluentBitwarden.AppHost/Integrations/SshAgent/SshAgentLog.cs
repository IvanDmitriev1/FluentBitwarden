using Microsoft.Extensions.Logging;

namespace FluentBitwarden.AppHost.Integrations.SshAgent;

internal static partial class SshAgentLog
{
    [LoggerMessage(EventId = 1300, Level = LogLevel.Error, Message = "SSH agent server loop failed.")]
    public static partial void AgentLoopFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1301, Level = LogLevel.Warning, Message = "SSH agent pipe accept failed.")]
    public static partial void AgentAcceptFailed(this ILogger logger, Exception exception);
}
