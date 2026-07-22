# AGENTS.md — FluentBitwarden

Guidebook for anyone (human or agent) writing code in this repository: how it is laid out, why, and
what the house patterns are.

Per-project guides live next to the code and go deeper:
[AppHost](FluentBitwarden.AppHost/AGENTS.md) ·
[Ui](FluentBitwarden.Ui/AGENTS.md) ·
[Contracts](FluentBitwarden.Contracts/AGENTS.md) ·
[Platform](FluentBitwarden.Platform/AGENTS.md) ·
[BitwardenApi](BitwardenApi/AGENTS.md)

## What this is

A native Windows Bitwarden client: WinUI 3 on .NET 10, split across several cooperating processes,
written to stay AOT- and trim-friendly. Vault data is cached locally in SQLite and decrypted only in
memory in the host process.

## Repo map

| Path | What lives there | Go here when… |
| --- | --- | --- |
| `BitwardenApi/` | Bitwarden server calls (HTTP + SignalR) and crypto primitives. No dependency on any FluentBitwarden project. | You need to talk to the Bitwarden server, or touch KDF / `EncString` / DTOs. |
| `FluentBitwarden.Contracts/` | The cross-process vocabulary: client interfaces, IPC message ids, request/response records. | You are adding or changing anything that crosses a process boundary. |
| `FluentBitwarden.Platform/` | Shared Windows infrastructure: named-pipe IPC, settings, site icons, clipboard, process helpers, diagnostics. | You need reusable Windows plumbing, not a feature. |
| `FluentBitwarden.AppHost/` | The headless long-running host. Owns local data, sessions, and every business module. | You are implementing behaviour: vault, accounts, unlock, SSH agent, passkeys, browser fill. |
| `FluentBitwarden.Ui/` | The WinUI 3 presentation process: pages, view models, controls, styles. | You are changing what the user sees. |
| `FluentBitwarden.CommandPalette/` | PowerToys Command Palette extension. | Palette commands and pages. |
| `FluentBitwarden.BrowseProxy/` | Browser Native Messaging bridge (stdio helper launched by the browser). | Browser extension ↔ AppHost transport. |
| `FluentBitwarden.ComServer/` | C++/WinRT WebAuthn plugin COM server. | Windows passkey plugin surface. |
| `FluentBitwarden.Package/` | MSIX packaging project. | Manifest, app entries, packaging. |
| `BrowserExtension/` | Manifest V3 extension, TypeScript + Vite + pnpm. | Extension UI and content scripts. |

Note: `FluentBitwarden.Ui` uses root namespace `FluentBitwarden`, **not** `FluentBitwarden.Ui`.
A file in `FluentBitwarden.Ui/Infrastructure/Clients/` is in namespace
`FluentBitwarden.Infrastructure.Clients`.

## Why it is split into processes

`FluentBitwarden.AppHost.exe` holds the decrypted vault and the account keys, and it must outlive any
window: the tray icon, the SSH agent, the passkey plugin, and the browser bridge all keep working
after the user closes the UI. `FluentBitwarden.Ui.exe` is therefore disposable — it can be closed,
killed, and relaunched without losing the unlocked session.

That split is the reason `FluentBitwarden.Contracts` exists. Every crossing between processes is an
explicit message with a fixed id and a serializable payload, so no accidental coupling can sneak in.

| Pipe | Server | Clients | Purpose |
| --- | --- | --- | --- |
| `LOCAL\FluentBitwarden.v2` | AppHost | UI, COM server, BrowseProxy | Account, vault, Windows Hello, passkey, lifecycle. |
| `LOCAL\FluentBitwarden.Ui.v2` | UI | AppHost | User-facing prompts: SSH approval, passkey selection. |

The AppHost also serves the OpenSSH-compatible `openssh-ssh-agent` pipe, which speaks the OpenSSH
agent protocol, not ours.

## Dependency direction

```
BitwardenApi  <-  Contracts  <-  Platform  <-  { AppHost, Ui, BrowseProxy, CommandPalette }
```

Arrows never reverse. In particular **AppHost and Ui never reference each other** — they only meet in
`Contracts` and talk over IPC. Inside AppHost the layering is enforced by NsDepCop and a breach fails
the build; see [FluentBitwarden.AppHost/AGENTS.md](FluentBitwarden.AppHost/AGENTS.md).

## Build and quality gates

Build the way CI does ([build.yml](.github/workflows/build.yml)):

```powershell
msbuild FluentBitwarden.slnx /restore /m /p:Configuration=Debug /p:Platform=x64
```

The gates that will bite you, all set centrally in [Directory.Build.props](Directory.Build.props) and
[.editorconfig](.editorconfig):

- `TreatWarningsAsErrors=true` and `EnforceCodeStyleInBuild=true` — a style warning is a broken build.
- `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=preview`, `IsAotCompatible=true`.
- `NSDEPCOP01` (namespace boundary breach) is an **error**.
- Package versions live only in [Directory.Packages.props](Directory.Packages.props) (Central Package
  Management). Never write `Version=` on a `PackageReference`.
- Style: file-scoped namespaces, `using` directives outside the namespace with `System` first,
  `_camelCase` private fields, `I`-prefixed interfaces, `PascalCase` constants.

`ConfigureAwait.Fody` rewrites awaits at build time, which is why CA2007 is switched off. Do **not**
write `.ConfigureAwait(false)` by hand — annotate the type instead:

```csharp
[Fody.ConfigureAwait(false)]
internal sealed class FooService(IBarClient bar) { /* ... */ }
```

## Cross-cutting conventions

**Dependency injection.** Primary constructors, constructor injection only. Implementations are
`internal sealed` unless something outside the assembly genuinely needs them. Every area exposes one
registration extension and the composition root is a flat list of those calls:

```csharp
internal static class FooServiceCollectionExtensions
{
    public static IServiceCollection AddFooServices(this IServiceCollection services)
    {
        services.AddSingleton<IFooStore, FooStore>();
        return services;
    }
}
```

See [Program.cs](FluentBitwarden.AppHost/Program.cs) for the host and
[App.xaml.cs](FluentBitwarden.Ui/App.xaml.cs) for the UI. Both build the provider with validation in
`DEBUG`, so a missing or captive dependency fails at startup rather than at first use.

**Logging.** Source-generated `LoggerMessage` methods in a `partial static class` named `<Area>Log`,
one per area — for example [VaultWorkspaceLog.cs](FluentBitwarden.AppHost/Modules/Vault/Workspace/VaultWorkspaceLog.cs):

```csharp
internal static partial class FooLog
{
    [LoggerMessage(EventId = 1300, Level = LogLevel.Error, Message = "Foo failed.")]
    public static partial void FooFailed(this ILogger logger, Exception exception);
}
```

No interpolated strings in log calls, and never log secrets, keys, or decrypted vault content.

**AOT and trimming.** No reflection-based serialization anywhere: MemoryPack for IPC, source-generated
`JsonSerializerContext` for HTTP, Dapper.AOT for SQL. `JsonSerializerIsReflectionEnabledByDefault=false`
is set repo-wide, so a reflection-based `JsonSerializer` call throws at runtime in Debug exactly as it
would in a trimmed publish — always pass a `JsonTypeInfo`. Where reflection is unavoidable it is annotated
(`RequiresDynamicCode` / `RequiresUnreferencedCode`) and suppressed at the call site with a written
`Justification`. Keep those annotations when you edit such code.

**Strongly-typed ids.** `CipherId`, `FolderId`, `UserId`, … are `readonly partial struct`s generated by
StronglyTypedId. Never pass a bare `string` or `Guid` id across a method or process boundary.

**Comments.** Sparse, and about *why* — especially where a trade-off was accepted deliberately. See the
comment in `DownloadCipherAttachmentAsync` in
[VaultIpcHandler.cs](FluentBitwarden.AppHost/Modules/Vault/Ipc/VaultIpcHandler.cs) explaining why the
download deliberately runs outside the session gate. Match that density; do not narrate code that
already reads clearly.

---

## Recipe A — add an IPC message end-to-end

Worked example: a `GetFoo` request from the UI to the AppHost. (The real `GetCipher` message follows
exactly these steps: id `102` → `GetVaultCipherRequest` → `VaultIpcHandler.GetCipherAsync` →
`RemoteVaultClient.GetCipherAsync`.)

**1. Reserve the message id** in [IpcMessageTypes.cs](FluentBitwarden.Contracts/Modules/IpcMessageTypes.cs),
inside the block that owns it. Take the next free number in that block; never reuse a retired one.

```csharp
public static class Foo
{
    public const ushort GetFoo = 700;
}
```

**2. Add the request record** under `FluentBitwarden.Contracts/Modules/Foo/`:

```csharp
[MemoryPackable]
public readonly partial record struct GetFooRequest(
    [property: StronglyTypedIdFormatter<FooId>] FooId FooId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Foo.GetFoo;
}
```

The response type just has to be MemoryPack-serializable. Use `IpcVoid` when there is no result.

**3. Add the method to the client interface** (`Contracts/Modules/Foo/IFooClient.cs`). One interface per
module; `ValueTask`-returning; trailing `CancellationToken cancellationToken = default`.

```csharp
public interface IFooClient
{
    ValueTask<Foo?> GetFooAsync(GetFooRequest request, CancellationToken cancellationToken = default);
}
```

**4. Implement the server side** in the AppHost module at `Modules/Foo/Ipc/FooIpcHandler.cs`. The handler
implements the client interface *and* the marker `IIpcRequestsHandler`; the message id is discovered
from the request type's `MessageType`. Methods that take no request need an explicit attribute:

```csharp
[Fody.ConfigureAwait(false)]
internal sealed class FooIpcHandler(IFooStore store) : IFooClient, IIpcRequestsHandler
{
    public ValueTask<Foo?> GetFooAsync(GetFooRequest request, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(store.Find(request.FooId));

    [IpcMessageHandler(IpcMessageTypes.Foo.ListFoos)]
    public ValueTask<Foo[]> ListFoosAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(store.List());
}
```

**5. Register the handler** once in
[AppHostIpcServiceCollectionExtensions.cs](FluentBitwarden.AppHost/AppHostIpcServiceCollectionExtensions.cs)
(`.Add<FooIpcHandler>()`). Registering the same message id twice throws at startup, on purpose.

**6. Implement the client side** at `FluentBitwarden.Ui/Infrastructure/Clients/RemoteFooClient.cs`. These
stay thin — a single `SendAsync` call, no business logic:

```csharp
[Fody.ConfigureAwait(false)]
internal sealed class RemoteFooClient(IIpcClient client) : IFooClient
{
    public ValueTask<Foo?> GetFooAsync(GetFooRequest request, CancellationToken cancellationToken = default) =>
        client.SendAsync<GetFooRequest, Foo?>(request, cancellationToken);
}
```

**7. Register it** in [Ui/Infrastructure/ServiceCollectionExtensions.cs](FluentBitwarden.Ui/Infrastructure/ServiceCollectionExtensions.cs):
`services.AddSingleton<IFooClient, RemoteFooClient>();`

**8. For push instead of request/response**, implement `IIpcEventMessage` on the record, publish from the
AppHost with `IIpcEventPublisher.PublishAsync`, and subscribe in the UI with `IIpcEventClient.Subscribe`
(or await one with `WaitAsync`). See `VaultSessionStatusChangedEvent` for the shape.

## Recipe B — add an AppHost module

Modules are the unit of business behaviour in the host. Create
`FluentBitwarden.AppHost/Modules/Foo/` with this skeleton:

```
Modules/Foo/
  Abstractions/                    <- the only thing sibling modules may reference
    IFooService.cs
  Models/                          <- value types siblings are allowed to see
    FooSnapshot.cs
  Internal/                        <- helpers nobody outside the module may touch
  Ipc/FooIpcHandler.cs             <- Recipe A, step 4
  Persistence/                     <- only if the module owns tables
    Repositories/FooRepository.cs
    Mapping/FooMapper.cs
  FooService.cs                    <- implementation, internal sealed
  FooServiceCollectionExtensions.cs
```

Then:

1. Register the module: `AddFooServices()` inside its extension, and one `builder.Services.AddFooServices();`
   line in [Program.cs](FluentBitwarden.AppHost/Program.cs).
2. If other modules need it, expose it **only** through `Abstractions/` and `Models/` — those two
   namespaces are the module contract, and NsDepCop enforces it. Nothing else is reachable from outside.
3. Add a rule pair for the new module namespace in
   [config.nsdepcop](FluentBitwarden.AppHost/config.nsdepcop) so it may see its own internals.
4. Never reference `FluentBitwarden.AppHost.Application.*` from a module; the dependency runs the other way.

Details and the reasoning: [FluentBitwarden.AppHost/AGENTS.md](FluentBitwarden.AppHost/AGENTS.md).

## Recipe C — add a UI page

1. **View model** in `FluentBitwarden.Ui/ViewModels/Foo/FooPageViewModel.cs`:

```csharp
public sealed partial class FooPageViewModel(IFooClient fooClient) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    public partial Foo? SelectedFoo { get; set; }

    public async Task OnLoadingAsync(CancellationToken cancellationToken) =>
        SelectedFoo = await fooClient.GetFooAsync(new GetFooRequest(FooId.Empty), cancellationToken);

    public void OnUnloading() { }

    [RelayCommand]
    private void ClearFoo() => SelectedFoo = null;
}
```

Use `IPageLifecycleAware<TIntent>` instead when the page is navigated to with a payload.

2. **Page** in `Views/Foo/FooPage.xaml` (root element `navigation:LifecyclePage`) with code-behind that
   only wires the view model. WinUI constructs pages itself during `Frame.Navigate(pageType)`, so the
   constructor takes no parameters and pulls the view model out of the container:

```csharp
public sealed partial class FooPage : LifecyclePage
{
    public FooPage()
    {
        ViewModel = App.Current.GetRequiredService<FooPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public FooPageViewModel ViewModel { get; }
}
```

3. **Register the view model only** — pages are not in the container:
   `services.AddTransient<FooPageViewModel>();` in
   [ViewRegistration.cs](FluentBitwarden.Ui/Views/ViewRegistration.cs).

Details: [FluentBitwarden.Ui/AGENTS.md](FluentBitwarden.Ui/AGENTS.md).

---

## Where to find things

| Looking for… | Look in |
| --- | --- |
| Vault decrypt, sync, search, save | `FluentBitwarden.AppHost/Modules/Vault/Workspace/` |
| Unlock / lock / session lifetime | `FluentBitwarden.AppHost/Modules/Sessions/` |
| Sign-in, 2FA, token refresh, Windows Hello | `FluentBitwarden.AppHost/Modules/Accounts/` |
| SQL schema and migrations | `FluentBitwarden.AppHost/Infrastructure/Data/Migrations/` |
| Transactions and repositories | `FluentBitwarden.AppHost/Infrastructure/Data/` |
| Message ids and payload records | `FluentBitwarden.Contracts/Modules/` |
| Pipe framing, headers, dispatch | `FluentBitwarden.Platform/Ipc/` |
| Settings storage | `FluentBitwarden.Platform/Settings/`, `FluentBitwarden.Contracts/Settings/` |
| Server HTTP calls | `BitwardenApi/Identity/`, `BitwardenApi/Vault/` |
| KDF, `EncString`, key derivation | `BitwardenApi/Infrastructure/Cryptography/` |
| Pages and view models | `FluentBitwarden.Ui/Views/`, `FluentBitwarden.Ui/ViewModels/` |
| Reusable controls and styles | `FluentBitwarden.Ui/Controls/`, `FluentBitwarden.Ui/Styles/` |
