# AGENTS.md — FluentBitwarden.Platform

Shared Windows infrastructure used by every process: the named-pipe IPC stack, settings storage, file
logging, site icons, clipboard and process helpers.

Rule of thumb: **reusable Windows plumbing belongs here; feature behaviour does not.** If a type only
makes sense for one capability, it belongs in that capability's module (AppHost) or in the UI.

Read [the root guide](../AGENTS.md) first.

## Layout

```
Ipc/
  Abstractions/   IIpcClient, IIpcRequestsHandler, IIpcEventClient/Publisher, IpcMessageHandlerAttribute
  Transport/      wire format: message and response headers
  Internal/       dispatch plumbing, endpoint factories, subscriptions
  Services/       PipeIpcClient, PipeIpcServer, PipeIpcEventHub/Client, PipeClientsVerifier
  IpcConstants.cs, IpcRpcHandlerBuilder.cs, IpcServiceCollectionExtensions.cs
Settings/         ApplicationDataSettingsStore + Composition/ (composite keys)
Diagnostics/      file logging provider and sinks
SiteIcons/        favicon cache
Infrastructure/   Clipboard/, Extensions/, Integrations/, ProcessManager/
```

## IPC

Pipe names and the protocol version live in [Ipc/IpcConstants.cs](Ipc/IpcConstants.cs) —
`LOCAL\FluentBitwarden.v2` (AppHost requests), `LOCAL\FluentBitwarden.Events.v2` (AppHost events),
`LOCAL\FluentBitwarden.Ui.v2` (UI prompts).

**One request per connection.** The client connects, writes a header plus MemoryPack payload, reads the
response, and disconnects. There is no multiplexing and no session state on the pipe.

**Server registration** goes through [`IpcRpcHandlerBuilder`](Ipc/IpcRpcHandlerBuilder.cs):

```csharp
services.AddIpcServer(
    IpcConstants.AppHostPipeName,
    handlers => handlers
        .Add<FooIpcHandler>()
        .Add<BarIpcHandler>());
```

`Add<THandler>()` reflects over the handler's methods once at startup. A method is an endpoint when its
request parameter implements `IIpcRequestMessage` (the id comes from the type's `MessageType`), or when
it carries `[IpcMessageHandler(id)]` — needed for methods that take no request. Registering the same id
twice throws at startup deliberately: a silent overwrite would be far worse to debug.

**Authentication** defaults to `IpcAuthenticationLevel.SamePackage`, checked by `PipeClientsVerifier`.
Lower it only with a written reason.

**Events** are one-way: `IIpcEventPublisher.PublishAsync` on the server, `IIpcEventClient.Subscribe` /
`WaitAsync` on the client.

## Wire format

The header types in [Ipc/Transport](Ipc/Transport) *are* the protocol: protocol version, message type,
payload length. Changing them is a breaking protocol change, and `FluentBitwarden.ComServer` (C++/WinRT)
mirrors this framing by hand for its passkey messages — update both, or the passkey plugin breaks
silently. Bump `IpcConstants.ProtocolVersion` and the pipe names together if the framing changes shape.

## Trimming and AOT annotations

Handler discovery and generic response deserialization are the one place where reflection survives. Those
APIs carry `[RequiresDynamicCode]` / `[RequiresUnreferencedCode]`, and generic parameters carry
`[DynamicallyAccessedMembers(...)]`. Callers suppress with a written `Justification` (see
`AddAppHostIpc`). Keep the annotations when you edit these paths — removing one turns a loud build
warning into a runtime failure in a published build.

## Settings

`ISettingsStore` (defined in `Contracts`) is implemented here by `ApplicationDataSettingsStore` over
Windows `ApplicationData`. Keys are typed (`SettingKey<T>`); grouped values use `CompositeSettingKey` and
`ICompositeSettingsStore` so a related set is written atomically. Add new keys next to the feature that
owns them, not in a global bucket.

## Logging

`AddAppLogging("<process-name>")` wires the file logger provider used by every process. Diagnostics code
lives in `Diagnostics/`; the message definitions belong to the area that logs them.
