# FluentBitwarden.AppHost

## Scope and role

Root instructions apply. This headless, long-running process owns the unlocked session, local database, tray/message loop, and business modules. It never owns a window: it asks the UI through IPC. Program.cs is the composition root; keep new behavior in a module and register it through that module's AddXxxServices() extension.

## Local architecture

- Application/ coordinates activation, tray, and session hosting.
- Modules/<feature>/ owns feature behavior. Siblings may use only another module's Abstractions/ and Models/ namespaces.
- Infrastructure/ is a leaf for data access and shared host services. Modules must not reach into Infrastructure.Data.Implementations.
- [config.nsdepcop](config.nsdepcop) enforces these rules. Disallowed rules take precedence; do not widen the documented Infrastructure.Data exception, which exists only for UnitOfWork composition.

An IPC handler implements the matching Contracts client interface and IIpcRequestsHandler, then is registered once in [AppHostIpcServiceCollectionExtensions.cs](AppHostIpcServiceCollectionExtensions.cs). See the Contracts and Platform guides for message and transport rules.

## Session state and persistence

`AppSession` owns the active account and its unlocked vault. Its singleton `ActiveSessionManager` keeps one immutable state record and derives `NotAuthenticated`, `Locked`, or `Unlocked` from the account and vault lifetime. Token caching is independent: the singleton `SessionTokenCache` holds refreshed tokens by account and server environment in memory, while the scoped access-token provider supplies the current `IAccountService` refresh callback. Protected refresh-token persistence remains in the Account module. Token expiry and refresh rejection do not change session status; refreshes do not update its timestamp or signal unlock waiters.

An unlocked-session lease captures the account selected when the lease is created and retains access to that vault lifetime. Locking removes the lifetime from active state immediately, so new leases wait, then requests vault disposal. Existing leases remain usable until each is disposed; the vault is disposed once the final lease is released. Switching accounts uses `LockAsync` followed by `Unlock`; a failed unlock leaves the previous account selected and locked.

SQLite access uses Dapper.AOT. A UnitOfWork owns one transaction; call SaveChanges() to commit. Repositories contain raw SQL, mapping lives beside them, and encrypted columns stay encrypted until the workspace decrypts them with session keys. Add migrations under Infrastructure/Data/Migrations/ as YYYYMMDDNNNN_description.sql; never edit or rename a shipped script because DbUp tracks it by name.

## Verification and completion

For AppHost or migration changes, run the repository CI build from the root guide. Session behavior and module registration are covered by `tests/FluentBitwarden.AppHost.IntegrationTests`; verify namespace-boundary compliance and migration immutability in addition to the root completion rules.
