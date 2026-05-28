namespace FluentBitwarden.Contracts.Session.Models;

[MemoryPackable(SerializeLayout.Explicit)]
public readonly partial record struct GetAccountsResponse(
    [property: MemoryPackOrder(0)]
    AccountProfile[] Accounts);