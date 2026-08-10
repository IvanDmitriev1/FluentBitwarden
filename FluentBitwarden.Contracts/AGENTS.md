# FluentBitwarden.Contracts

## Scope and role

Root instructions apply. This project is the vocabulary shared between processes: client interfaces, IPC message IDs, request/response/event payloads, and settings types. It is a leaf that references BitwardenApi only; [config.nsdepcop](config.nsdepcop) forbids dependencies on AppHost, Platform, or UI.

## Local rules

Modules/IpcMessageTypes.cs is the message-ID registry. Keep IDs in the owning module's reserved block and take the next free value; never reuse a retired ID. Put a module's IXxxClient interface and payloads under Modules/<Module>/. Interfaces return ValueTask, accept one request record or none, and end with CancellationToken cancellationToken = default.

Use [MemoryPackable] payloads. Small shapes are eadonly partial record struct; use a record class only when its shape requires it. Implement IIpcRequestMessage for request/response and IIpcEventMessage for pushes. Strongly typed ID parameters require [property: StronglyTypedIdFormatter<T>]. Return a normal empty/nullable result for an expected locked vault state instead of treating it as a transport exception.

Adding a member to a MemoryPack payload changes the wire format. All managed processes ship in one MSIX, but passkey messages are mirrored by FluentBitwarden.ComServer; read that guide and update the native binary representation when applicable.

## Verification and completion

Run the repository CI build and validate every handler and client consumer of a changed contract. Check ID uniqueness, MemoryPack shape, and native passkey compatibility where relevant.