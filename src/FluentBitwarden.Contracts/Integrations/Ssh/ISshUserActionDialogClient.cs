using FluentBitwarden.Contracts.Modules.Ssh;

namespace FluentBitwarden.Contracts.Integrations.Ssh;

public interface ISshUserActionDialogClient
{
    Task<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken = default);
}
