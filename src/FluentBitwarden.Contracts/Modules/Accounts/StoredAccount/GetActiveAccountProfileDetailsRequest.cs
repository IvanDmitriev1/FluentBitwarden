namespace FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

[MemoryPackable]
public readonly partial record struct GetActiveAccountProfileDetailsRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Account.GetActiveAccountProfileDetails;
}
