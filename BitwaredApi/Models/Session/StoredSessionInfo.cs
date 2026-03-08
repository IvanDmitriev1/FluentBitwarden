namespace BitwaredApi.Models.Session;

public sealed record StoredSessionInfo(
    string AccountId,
    string Email,
    BitwardenEnvironment Environment,
    bool IsLocked,
    bool CanUnlockWithMasterPassword);
