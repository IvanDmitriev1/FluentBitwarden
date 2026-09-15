# FluentBitwarden.Platform

## Scope and role

Root instructions apply. This project supplies reusable Windows plumbing: named-pipe IPC, settings storage, diagnostics, site icons, clipboard, process helpers, and integrations. Feature behavior belongs in an AppHost module or the UI, not here.

## IPC and compatibility

Ipc/ contains abstractions, transport headers, dispatch internals, pipe services, constants, and registration extensions. A request connection carries one request and one response; it is not multiplexed or sessionful. Register handlers through IpcRpcHandlerBuilder.Add<THandler>(); duplicate IDs intentionally fail at startup. The default IPC authentication level is SamePackage; lower it only with a written reason.

Ipc/Transport headers are protocol. A framing change must update IpcConstants.ProtocolVersion, pipe names, and the mirrored FluentBitwarden.ComServer implementation. Events are one-way through IIpcEventPublisher and IIpcEventClient.

Handler discovery and generic response deserialization are the documented reflection/AOT boundary. Preserve RequiresDynamicCode, RequiresUnreferencedCode, DynamicallyAccessedMembers, and written call-site justifications.

## Settings and diagnostics

ISettingsStore from Contracts is implemented by ApplicationDataSettingsStore. Use typed SettingKey<T> values; use CompositeSettingKey and ICompositeSettingsStore for atomic grouped writes, and put a key beside the feature that owns it. AddAppLogging configures shared file logging; message definitions remain with the area that emits them.

## Verification and completion

Run the repository CI build for Platform changes. For transport changes, validate registration, authorization level, framing, and every managed/native consumer.