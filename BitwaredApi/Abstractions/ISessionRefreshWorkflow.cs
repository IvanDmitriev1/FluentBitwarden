using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions;

public interface ISessionRefreshWorkflow
{
    ValueTask<SessionRefreshOutcome> RefreshAsync(
        SessionRefreshRequest request,
        CancellationToken cancellationToken = default);
}
