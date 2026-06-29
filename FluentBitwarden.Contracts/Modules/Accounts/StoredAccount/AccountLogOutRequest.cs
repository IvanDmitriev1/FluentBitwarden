namespace FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

[MemoryPackable]
public readonly partial record struct AccountLogOutRequest(UserId AccountId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Account.Logout;
}
