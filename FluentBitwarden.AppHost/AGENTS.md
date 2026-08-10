# FluentBitwarden.AppHost

## Scope and role

Root instructions apply. This headless, long-running process owns the unlocked session, local database, tray/message loop, and business modules. It never owns a window: it asks the UI through IPC. Program.cs is the composition root; keep new behavior in a module and register it through that module's AddXxxServices() extension.

## Local architecture

- Application/ coordinates activation, tray, and session hosting.
- Modules/<feature>/ owns feature behavior. Siblings may use only another module's Abstractions/ and Models/ namespaces.
- Infrastructure/ is a leaf for data access and shared host services. Modules must not reach into Infrastructure.Data.Implementations.
- [config.nsdepcop](config.nsdepcop) enforces these rules. Disallowed rules take precedence; do not widen the documented Infrastructure.Data exception, which exists only for UnitOfWork composition.

An IPC handler implements the matching Contracts client interface and IIpcRequestsHandler, then is registered once in [AppHostIpcServiceCollectionExtensions.cs](AppHostIpcServiceCollectionExtensions.cs). See the Contracts and Platform guides for message and transport rules.

## Sessions and persistence

Modules/Sessions owns the unlocked vault through IVaultSessionManager. Reads may use TryGetUnlockedSession and return an empty result when locked. Mutations use WithSessionAsync with a locked result so a normal lock transition does not escape as an IPC exception. Only long-running transfers may resolve the session once and run outside that gate; preserve the documented attachment-download exception. A vault handle is immutable: sync/save creates a replacement and swaps it with ReplaceVault.

SQLite access uses Dapper.AOT. A UnitOfWork owns one transaction; call SaveChanges() to commit. Repositories contain raw SQL, mapping lives beside them, and encrypted columns stay encrypted until the workspace decrypts them with session keys. Add migrations under Infrastructure/Data/Migrations/ as YYYYMMDDNNNN_description.sql; never edit or rename a shipped script because DbUp tracks it by name.

## Verification and completion

For AppHost or migration changes, run the repository CI build from the root guide. There are no AppHost test projects in the repository. Confirm module registration, namespace-boundary compliance, session behavior, and migration immutability in addition to the root completion rules.