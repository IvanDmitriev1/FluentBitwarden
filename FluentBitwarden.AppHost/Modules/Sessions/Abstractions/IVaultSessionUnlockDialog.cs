namespace FluentBitwarden.AppHost.Modules.Sessions.Abstractions;

/// <summary>
/// Brings the UI process forward to ask the user to unlock, and completes once they have.
/// </summary>
internal interface IVaultSessionUnlockDialog
{
    Task WaitUntilUnlockAsync(CancellationToken cancellationToken);
}
