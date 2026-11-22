namespace FluentBitwarden.Contracts.Modules.Passkey.Models;

[MemoryPackable]
public readonly partial record struct PasskeySelectCredentialResponse(
    [property: StronglyTypedIdFormatter<CipherId>] CipherId CipherId);
