using FluentBitwarden.Contracts.Modules.Ssh;

namespace FluentBitwarden.Contracts.Infrastructure.UserDialog;

public interface IUserDialogClient
{
    ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken = default);
}
