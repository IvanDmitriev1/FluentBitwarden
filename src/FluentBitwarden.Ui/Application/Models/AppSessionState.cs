namespace FluentBitwarden.Application.Models;

public enum AppSessionState
{
    /// <summary>
    /// App has not resolved the real session state yet,
    /// or the window is closed,
    /// or a new flow is currently running.
    /// </summary>
    Unknown,

    /// <summary>
    /// There is no usable local session/account state.
    /// </summary>
    LoggedOut,

    /// <summary>
    /// Account data exists, but the vault/session is locked.
    /// </summary>
    Locked,

    /// <summary>
    /// An account is available and unlocked.
    /// </summary>
    Unlocked,
}
