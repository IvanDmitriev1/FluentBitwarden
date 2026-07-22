# AGENTS.md — FluentBitwarden.AppHost

The headless, long-running host process. It owns the local database, the unlocked session (keys and
decrypted vault), the tray icon and message loop, and every business module. It has no windows: when
something must be shown to the user it asks the UI process over IPC.

Read [the root guide](../AGENTS.md) first for repo-wide conventions and the IPC recipe.

## Layout

```
Program.cs                     composition root: single-instance guard, DI wiring, host run
AppHostIpcServiceCollectionExtensions.cs   every IPC handler is registered here

Application/                   host concerns: activation, tray, session hosting
Infrastructure/                data access, security helpers, shared services — a leaf layer
Modules/                       business behaviour, one folder per capability
```

## The layer law

Enforced by [config.nsdepcop](config.nsdepcop). `NSDEPCOP01` is an **error**
(see [.editorconfig](../.editorconfig)), so a breach fails the build rather than review.

- **`Application.*`** drives modules only through `Modules.*.Abstractions.*` and `Modules.*.Models.*`.
  It may use `Infrastructure.*` freely.
- **`Modules.*`** own their internals completely, and see *sibling* modules only through those same two
  namespaces. Those two lines in the config are the module contract.
- **`Infrastructure.*`** is a leaf: it may not reference `Application.*`, and modules may not reach into
  `Infrastructure.Data.Implementations.*`.
- `Disallowed` beats `Allowed` in NsDepCop, so the tripwire rules at the bottom of the config keep the
  invariants true even if someone widens an `Allowed` rule above.

Why bother: modules stay swappable and independently readable, and "just reach across for one field"
stops being possible without an explicit, reviewable config change.

There is exactly one accepted exception, documented in the config: the exact namespace
`Infrastructure.Data` may name a few repository types, because `UnitOfWork` composes every module's
repositories into a single transaction. Only its construction touches concrete types; its surface
exposes abstractions. Don't widen that grant.

## Module anatomy

| Folder | Meaning |
| --- | --- |
| `Abstractions/` | Public surface for sibling modules and `Application`. Interfaces and result types. |
| `Models/` | Value types siblings are allowed to pass around. |
| `Internal/` | Helpers private to the module. Nothing outside may reference these. |
| `Ipc/` | The module's `IIpcRequestsHandler`, implementing its `Contracts` client interface. |
| `Persistence/` | `Repositories/`, `Mapping/`, `Parsing/`. Only for modules that own tables. |
| `XxxServiceCollectionExtensions.cs` | The module's single registration entry point. |
| `Xxx.cs` at module root | The implementations themselves, `internal sealed`. |

Existing modules to copy from: `Vault` (largest, has persistence and workspace), `Sessions` (owns the
unlock/lock state machine), `Accounts`, `BrowserExtension`, `Passkey`, `SshAgent`.

## The session gate

The unlocked session — user key, org keys, decrypted vault — lives in `Modules/Sessions`, behind
[`IVaultSessionManager`](Modules/Sessions/Abstractions/IVaultSessionManager.cs). Two access shapes,
and picking the right one matters:

```csharp
// Read: cheap, non-blocking, degrades to an empty result when locked.
public ValueTask<Foo[]> GetFoosAsync(CancellationToken cancellationToken = default) =>
    ValueTask.FromResult(
        sessionManager.TryGetUnlockedSession(out var session) ? session.Vault.GetFoos() : []);

// Mutation: runs under the transition gate, so it serializes against unlock and lock.
public async ValueTask<Foo?> SaveFooAsync(SaveFooRequest request, CancellationToken cancellationToken = default) =>
    await sessionManager.WithSessionAsync<Foo?>(
        async (session, ct) => await workspace.SaveAsync(session, request.Foo, ct),
        lockedResult: null,
        cancellationToken);
```

`lockedResult` is what the caller gets when the vault is locked — mutations never throw across the IPC
boundary just because the user locked mid-flight.

Deliberate exception: long-running transfers (attachment download) resolve the session once and then run
*outside* the gate, because holding it for the length of a transfer would block the tray's Lock button.
See the comment in [VaultIpcHandler.cs](Modules/Vault/Ipc/VaultIpcHandler.cs).

The vault handle inside a session is immutable; a sync or save produces a new one and the session
swaps it in with `ReplaceVault`.

## Persistence

SQLite via Dapper.AOT. One transaction per unit of work, composed in
[`UnitOfWork`](Infrastructure/Data/UnitOfWork.cs):

```csharp
using var unitOfWork = unitOfWorkFactory.Create();
unitOfWork.FooRepository.Write(userId, foos);
unitOfWork.SaveChanges();   // commit; skipping this rolls back on Dispose
```

Repositories derive `BaseRepository(SqliteTransaction)` and expose `Connection` / `Transaction`. They
hold raw SQL and nothing else — no mapping logic inline. Mapping lives beside them in
`Persistence/Mapping/XxxMapper.cs` as a static class with `readonly record struct` row and parameter
types:

```csharp
internal static class FooMapper
{
    public readonly record struct FooRow(string FooId, long CreatedUnixMs);

    public static Foo ToDomain(in FooRow row) => new()
    {
        Id = FooId.Parse(row.FooId, CultureInfo.InvariantCulture),
        Created = row.CreatedUnixMs.ToDateTimeOffsetFromUnixMs(),
    };

    public readonly record struct FooInsertParameters(string UserId, string FooId, long CreatedUnixMs);

    public static FooInsertParameters ToInsertParameters(string userId, in Foo foo) => new(
        UserId: userId,
        FooId: foo.Id.ToString(),
        CreatedUnixMs: foo.Created.ToUnixMs());
}
```

Encrypted columns stay encrypted at rest: they are stored as `byte[]` from `EncString`, and decryption
happens in the workspace with the session keys — never in a repository.

**Schema changes**: add a new file to [Infrastructure/Data/Migrations](Infrastructure/Data/Migrations)
named `YYYYMMDDNNNN_description.sql`. DbUp tracks applied scripts by name, so never edit or rename one
that has shipped.

## Startup

[Program.cs](Program.cs) is deliberately boring:

1. Single-instance guard via `AppInstance.FindOrRegisterForKey`; a second launch redirects activation and exits.
2. `Host.CreateApplicationBuilder`, with `ValidateOnBuild` + `ValidateScopes` in `DEBUG`.
3. One `AddXxxServices()` line per area, then `AddAppHostIpc()`.
4. Activation handlers wired, `IAppSetupService.Initialize()`, `host.Run()`.

New behaviour belongs in a module with its own registration extension — not in `Program.cs`.
