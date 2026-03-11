using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions;

public interface IMasterPasswordUnlockWorkflow
{
    ValueTask<MasterPasswordUnlockOutcome> UnlockAsync(
        MasterPasswordUnlockRequest request,
        CancellationToken cancellationToken = default);
}
