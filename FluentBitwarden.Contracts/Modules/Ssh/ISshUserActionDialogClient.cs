

namespace FluentBitwarden.Contracts.Modules.Ssh;

public interface ISshUserActionDialogClient
{
    ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken = default);
}
