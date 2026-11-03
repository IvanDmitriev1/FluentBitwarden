# FluentBitwarden.AppHost

## Scope and role

Root instructions apply. This headless, long-running process owns the unlocked session, local database, tray/message loop, and business modules. It never owns a window: it asks the UI through IPC. Program.cs is the composition root; keep new behavior in a module and register it through that module's AddXxxServices() extension.

## Local architecture

- AppHostLifecycleService.cs coordinates startup and shutdown at the root. Application/ owns activation and tray coordination; Application/Abstractions/ exposes application-wide capabilities such as termination. Infrastructure/Tray/ contains the native tray window, menu, and icon.
- Modules/<feature>/ owns feature behavior. Siblings may use only another module's Abstractions/ and Models/ namespaces.
- Infrastructure/ is a leaf for data access and shared host services. Modules must not reach into Infrastructure.Data.Implementations.
- [config.nsdepcop](config.nsdepcop) enforces these rules. Disallowed rules take precedence; do not widen the documented Infrastructure.Data exception, which exists only for UnitOfWork composition.

An IPC handler implements the matching Contracts client interface and IIpcRequestsHandler, then is registered once in [AppHostIpcServiceCollectionExtensions.cs](AppHostIpcServiceCollectionExtensions.cs). See the Contracts and Platform guides for message and transport rules.

## Session state and persistence

`AppSession` owns the active account and its unlocked vault. Its singleton `ActiveSessionManager` keeps one immutable state record and derives `NotAuthenticated`, `Locked`, or `Unlocked` from the account and vault lifetime. Token caching is independent: the singleton `SessionTokenCache` holds refreshed tokens by account and server environment in memory, while the scoped access-token provider supplies the current `IAccountService` refresh callback. Protected refresh-token persistence remains in the Account module. Token expiry and refresh rejection do not change session status; refreshes do not update its timestamp or signal unlock waiters.

Each DI scope's session accessor captures one session state on first access and retains its vault lifetime until scope disposal. Accessors in that scope always refer to the captured state and never follow session changes; lock or sign-out revokes it and requests cleanup, which waits until the final scoped reference is released. The hosted service explicitly stops the session store during host shutdown. SSH identities and passkey selection await unlock readiness before first reading their injected accessor. Once captured, the state stays fixed; locking between readiness and capture fails without retrying. Switching accounts uses `LockAsync` followed by `Unlock`; a failed unlock leaves the previous account selected and locked.

SQLite access uses Dapper.AOT. A UnitOfWork owns one transaction; call SaveChanges() to commit. Repositories contain raw SQL, mapping lives beside them, and encrypted columns stay encrypted until the workspace decrypts them with session keys. Add migrations under Infrastructure/Data/Migrations/ using the current numbered sequence, such as 006_description.sql; never edit or rename a shipped script because DbUp tracks it by name. Initialization derives current journal aliases from discovered scripts when historical names share the same migration namespace and suffix and differ only by a 12-digit timestamp prefix versus the current 3-digit prefix, preserving the original Applied times and historical rows. For missing journal entries on numbered scripts 001 through 005, it replays the discovered scripts in memory and repairs only when every table and explicit index created by a script already exists with a matching definition; it leaves fully absent scripts for DbUp and stops on partial or mismatched schema. Scripts after 005 are not automatically schema-reconciled.

## Verification and completion

For AppHost or migration changes, run the repository CI build from the root guide. Session behavior and module registration are covered by `tests/FluentBitwarden.AppHost.IntegrationTests`; verify namespace-boundary compliance and migration immutability in addition to the root completion rules.
