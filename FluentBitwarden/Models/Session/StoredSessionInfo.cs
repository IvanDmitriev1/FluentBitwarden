using BitwaredApi;

namespace FluentBitwarden.Models.Session;

internal sealed record StoredSessionInfo(
    string AccountId,
    string Email,
    BitwardenEnvironment Environment,
    bool IsLocked,
    bool CanUnlockWithMasterPassword);
