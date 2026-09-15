using Microsoft.Extensions.Logging;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Services;

internal static partial class SshAgentLog
{
    [LoggerMessage(EventId = 1300, Level = LogLevel.Error, Message = "SSH agent server loop failed.")]
    public static partial void AgentLoopFailed(this ILogger logger, Exception exception);
}
