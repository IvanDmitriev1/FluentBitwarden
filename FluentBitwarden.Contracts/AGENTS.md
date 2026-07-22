# AGENTS.md — FluentBitwarden.Contracts

The vocabulary shared between processes: client interfaces, IPC message ids, and the request / response /
event payloads. If a type crosses a process boundary, it is defined here.

Read [the root guide](../AGENTS.md) first — Recipe A walks a new message through every project.

## The one rule

`Contracts` is a leaf. It references `BitwardenApi` and nothing else of ours. Depending on `AppHost`,
`Platform`, or `Ui` is forbidden and enforced by [config.nsdepcop](config.nsdepcop) as a build error.

That is what makes it safe for every process — including the C++ COM server, which mirrors the small
binary subset it needs — to agree on the same shapes.

## Layout

```
Infrastructure/Ipc/Abstractions/   IIpcMessage, IIpcRequestMessage, IIpcEventMessage
Modules/IpcMessageTypes.cs         the message id registry
Modules/<Module>/                  client interface + payload records for that module
Settings/                          ISettingsStore and setting key/value types
GlobalUsings.cs                    MemoryPack, BitwardenApi primitives, IPC abstractions
```

## Message ids

All ids live in [Modules/IpcMessageTypes.cs](Modules/IpcMessageTypes.cs), grouped into a nested static
class per module with a reserved range (`System` 1, `Passkey` 50, `Vault` 100, `Account` 200,
`WindowsHello` 300, `Ui` 400, `Browser` 500, `Session` 600).

Take the next free number inside the owning block. Never reuse a retired id — an old client build would
silently call the wrong handler.

## Payload shape

```csharp
[MemoryPackable]
public readonly partial record struct GetFooRequest(
    [property: StronglyTypedIdFormatter<FooId>] FooId FooId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Foo.GetFoo;
}
```

- `readonly partial record struct` for small payloads; a `record` class is fine when the payload is large
  or polymorphic.
- `[MemoryPackable]` — the wire format is MemoryPack, never reflection-based serialization.
- Implement `IIpcRequestMessage` for request/response, `IIpcEventMessage` for pushes. Both carry the
  `static abstract ushort MessageType`, which is how the transport finds the id without a lookup table.
- Strongly-typed ids need `[property: StronglyTypedIdFormatter<T>]` on the parameter so MemoryPack knows
  how to write them.
- Events look identical apart from the interface:

```csharp
[MemoryPackable]
public readonly partial record struct FooChangedEvent(FooStatus Status) : IIpcEventMessage
{
    public static ushort MessageType => IpcMessageTypes.Foo.FooChanged;
}
```

## Client interfaces

One per module, named `IXxxClient`, describing what the *caller* can ask for. The AppHost handler and the
UI's `Remote*` client both implement it, which keeps the two sides honest.

```csharp
public interface IFooClient
{
    ValueTask<Foo[]> GetFoosAsync(CancellationToken cancellationToken = default);
    ValueTask<Foo?> GetFooAsync(GetFooRequest request, CancellationToken cancellationToken = default);
}
```

Conventions: `ValueTask`-returning, one request record parameter (or none), trailing
`CancellationToken cancellationToken = default`. Return a sensible empty value rather than throwing when
the vault is locked — the locked case is normal, not exceptional.

## Adding to a payload

Adding a member to an existing `[MemoryPackable]` record changes the wire format. All processes ship in
one MSIX so they upgrade together, but the C++ COM server mirrors part of the protocol by hand — if you
touch a passkey message, check `FluentBitwarden.ComServer` too.
