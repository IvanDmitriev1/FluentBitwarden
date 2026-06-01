using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Contracts.Modules.Ssh;

namespace FluentBitwarden.Contracts.Infrastructure.UserDialog;

public interface IUserDialogClient
{
    ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<Fido2Credential> SelectPasskeyCredential(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken);
}
