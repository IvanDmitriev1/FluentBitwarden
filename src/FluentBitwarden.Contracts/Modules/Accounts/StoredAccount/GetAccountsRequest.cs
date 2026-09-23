namespace FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

[MemoryPackable]
public readonly partial record struct GetAccountsRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Account.GetAccounts;
}
