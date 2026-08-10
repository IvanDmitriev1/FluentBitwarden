# FluentBitwarden agent guide

## Scope and precedence

This file governs the repository. A nested AGENTS.md supplements it; the closest applicable file takes priority. Read the local guide before planning or modifying a project. For cross-project work, read every affected guide. Explicit user instructions override these defaults except confirmed safety constraints and invariants.

## Portable system overview

FluentBitwarden is a work-in-progress native Windows Bitwarden client built with .NET 10 and WinUI 3. It keeps encrypted account data and a SQLite vault cache locally; decrypted vault data exists only in the long-running AppHost. The MSIX deploys an AppHost, WinUI UI, C++/WinRT WebAuthn COM server, browser Native Messaging bridge, and Command Palette extension. A Manifest V3 TypeScript browser extension connects through the bridge. The app is not production-ready; see [README.md](README.md).

## Repository map and instruction router

| Path | Responsibility | Read when |
| --- | --- | --- |
| [BitwardenApi/](BitwardenApi/AGENTS.md) | Bitwarden HTTP, SignalR, crypto, IDs, DTOs | Server calls, KDFs, encryption, JSON, shared primitives |
| [FluentBitwarden.Contracts/](FluentBitwarden.Contracts/AGENTS.md) | IPC interfaces, IDs, payloads | Any cross-process or settings contract |
| [FluentBitwarden.Platform/](FluentBitwarden.Platform/AGENTS.md) | Windows IPC, settings, diagnostics, helpers | Shared Windows infrastructure |
| [FluentBitwarden.AppHost/](FluentBitwarden.AppHost/AGENTS.md) | Sessions, data, and feature modules | Vault, account, unlock, passkey, SSH, browser behavior |
| [FluentBitwarden.Ui/](FluentBitwarden.Ui/AGENTS.md) | WinUI presentation | Pages, view models, controls, styles |
| [BrowserExtension/](BrowserExtension/AGENTS.md) | Manifest V3 extension | Browser background, content scripts, bundles |
| [FluentBitwarden.Package/](FluentBitwarden.Package/AGENTS.md) | MSIX manifest and composition | App entries, capabilities, packaging |

## System boundaries

Managed dependencies flow BitwardenApi <- Contracts <- Platform <- { AppHost, Ui, BrowseProxy, CommandPalette }. AppHost and Ui never reference each other; requests cross through Contracts and IPC. Contracts owns managed IPC vocabulary. The COM server mirrors the binary passkey subset; the browser extension mirrors its browser-native protocol.

Use strongly typed IDs at method and process boundaries. IPC uses MemoryPack; JSON uses source-generated JsonSerializerContext/JsonTypeInfo; SQL uses Dapper.AOT. Reflection-based JSON is disabled by [Directory.Build.props](Directory.Build.props). Keep trimming annotations and written suppressions on the documented reflection boundary. Package versions belong only in [Directory.Packages.props](Directory.Packages.props); do not put Version= on a PackageReference. Build and analyzer rules are enforced by [Directory.Build.props](Directory.Build.props) and [.editorconfig](.editorconfig); use ConfigureAwait.Fody annotations instead of .ConfigureAwait(false).

## Repository-wide commands

Run from the repository root in a Visual Studio Developer PowerShell or equivalent environment:

`powershell
nuget restore FluentBitwarden.ComServer\packages.config -PackagesDirectory packages -NonInteractive
msbuild FluentBitwarden.slnx /restore /m /p:Configuration=Release /p:Platform=x64 /p:AppxPackageSigningEnabled=false /p:GenerateAppxPackageOnBuild=false /v:minimal
`

This is the CI build in [.github/workflows/build.yml](.github/workflows/build.yml); it restores native packages and builds the complete solution without creating a signed MSIX. No test projects or test scripts are currently present, so no verified repository-wide test command exists.

## Global workflow and cross-project changes

Inspect nearby code and configuration, identify all affected projects/contracts, make the smallest coherent change, run narrow checks first, run the CI build for shared or cross-project work, review the diff, and report checks run or skipped.

For an IPC feature, reserve the ID and define the payload/interface in Contracts, implement/register the AppHost handler, and implement/register each consuming remote client. Do not reuse a retired ID. A pipe-framing, protocol-version, or passkey-encoding change must update and verify the matching ComServer code. Browser integration changes usually span BrowserExtension, BrowseProxy, Contracts, and AppHost; keep message types, version, validation, and dispatch aligned. Shipped AppHost migration scripts are immutable. State upgrade, deployment, and rollback implications for compatibility-sensitive work.

## Safety and completion

Never log, commit, serialize for diagnostics, or persist decrypted vault values, account keys, tokens, or other secrets. Keep encrypted data encrypted at rest. Do not add dependencies, change authentication/authorization behavior, alter package capabilities, publish/release, or make destructive migrations without explicit user approval. Report vulnerabilities through [SECURITY.md](SECURITY.md), not public issues.

Completion requires affected checks to pass or be reported as unrun, shared contracts validated in all consumers, no untracked generated output, behavior/command documentation updated when needed, and no unrelated diff.

## Planning and documentation

Separate verified facts, assumptions, and open questions. Do not invent files, APIs, schemas, or commands. Identify all affected projects; consider contracts, migrations, compatibility, deployment order, and rollback when relevant; pair implementation steps with verification. A plan is not implementation-ready while material facts are unknown.

- [README.md](README.md): product scope, prerequisites, and process roles.
- [.github/workflows/build.yml](.github/workflows/build.yml): authoritative CI build.
- [Directory.Build.props](Directory.Build.props), [.editorconfig](.editorconfig), and [Directory.Packages.props](Directory.Packages.props): shared .NET build, analyzer, and dependency rules.
- [SECURITY.md](SECURITY.md): vulnerability reporting.