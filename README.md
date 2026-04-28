# FluentBitwarden

FluentBitwarden is a work-in-progress native Windows Bitwarden client built with WinUI 3 and .NET 10. The project focuses on a Fluent-style desktop vault experience with local encrypted account data, offline vault caching, and a Windows passkey authenticator integration.

## Status

This repository is an active work in progress. Core sign-in, sync, unlock, and vault viewing paths are present, but the app should not be treated as a production-ready Bitwarden replacement yet.

## Features

- Bitwarden account sign-in with password and two-factor authentication flows.
- Vault sync through a dedicated `BitwardenApi` backend communication layer.
- Local SQLite cache for accounts, folders, collections, and ciphers.
- Local master-password unlock and decrypted vault viewing.
- Login, secure note, card, identity, SSH key, password, and TOTP display support.
- App settings for theme and lock behavior.
- Single-instance launch handling and tray icon support.
- Windows passkey assertion support through a C++/WinRT COM server.

## Tech Stack

- WinUI 3 and Windows App SDK for the desktop UI.
- .NET 10.
- SQLite, Dapper, and Dapper.AOT for local storage.
- `BitwardenApi` for identity, vault, attachment, and notification backend calls.
- C++/WinRT COM server.

## Requirements

- Windows 10 version 2004 or later for the main app; Windows 11 24H2 or later for passkey plugin.
- .NET 10 SDK.
- Windows SDK / Windows App SDK environment compatible with SDK version `10.0.26100.0`.
- Visual Studio with C++ desktop tooling when building the COM server or MSIX package.

## Build

Restore packages:

```powershell
dotnet restore
```

Build the x64 solution:

```powershell
dotnet build FluentBitwarden.slnx -p:Platform=x64
```

The solution contains the WinUI app, the `BitwardenApi` library, the C++/WinRT COM server, and the MSIX packaging project.

## License

FluentBitwarden is licensed under the MIT License. See [LICENSE.txt](LICENSE.txt).
