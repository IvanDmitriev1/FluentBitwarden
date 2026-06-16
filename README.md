[![Build](https://github.com/IvanDmitriev1/FluentBitwarden/actions/workflows/build.yml/badge.svg)](https://github.com/IvanDmitriev1/FluentBitwarden/actions/workflows/build.yml)

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
- Browser extension integration for login and TOTP autofill through browser Native Messaging.

## Tech Stack

- WinUI 3 and Windows App SDK for the desktop UI.
- .NET 10.
- SQLite, Dapper, and Dapper.AOT for local storage.
- MemoryPack over current-user named pipes for internal IPC.
- `BitwardenApi` for identity, vault, attachment, and notification backend calls.
- C++/WinRT COM server.
- Manifest V3 browser extension built with TypeScript and Vite.
- Browser Native Messaging through the `FluentBitwarden.BrowseProxy` bridge process.

## App and IPC Architecture

FluentBitwarden is packaged as a multi-process desktop app. The MSIX package declares a visible `MainApp` entry point plus hidden helper applications:

- `FluentBitwarden.AppHost.exe` is the long-running host process. It enforces the AppHost single instance, owns the tray/message loop, initializes local data, and hosts account, unlock, vault, passkey, Windows Hello, and SSH-agent services.
- `FluentBitwarden.Ui.exe` is the WinUI presentation process. It enforces its own single instance, owns windows and dialogs, and is launched or foregrounded by the AppHost for the main window or overlay prompts.
- `FluentBitwarden.ComServer.exe` is the native C++/WinRT WebAuthn plugin COM server. It handles Windows passkey plugin calls, decodes WebAuthn requests, and forwards assertion work to the AppHost.
- `FluentBitwarden.BrowseProxy.exe` is the browser Native Messaging bridge. Browser extensions launch it as a stdio helper, and it forwards browser credential requests to the AppHost through the shared IPC contracts.
- `FluentBitwarden.Contracts` contains the shared .NET IPC contracts: message ids, request/response models, named-pipe client/server services, and MemoryPack serialization setup. The COM server mirrors the small binary protocol subset it needs for passkey messages.

### Pipe Endpoints

Internal app traffic uses one request per named-pipe connection:

| Pipe | Server | Clients | Purpose |
| --- | --- | --- | --- |
| `LOCAL\FluentBitwarden.v2` | `FluentBitwarden.AppHost.exe` | UI process and COM server | Account, vault, Windows Hello, passkey assertion, and lifecycle requests. |
| `LOCAL\FluentBitwarden.Ui.v2` | `FluentBitwarden.Ui.exe` | AppHost process | User-facing prompts such as SSH approval and passkey credential selection. |

The AppHost also exposes the OpenSSH-compatible `openssh-ssh-agent` pipe for SSH-agent clients. That pipe uses the OpenSSH agent protocol, not the FluentBitwarden IPC protocol.

## Requirements

- Windows 10 version 2004 or later for the main app; Windows 11 24H2 or later for passkey plugin.
- .NET 10 SDK.
- Windows SDK / Windows App SDK environment compatible with SDK version `10.0.26100.0`.
- Visual Studio with C++ desktop tooling when building the COM server or MSIX package.

## License

FluentBitwarden is licensed under the MIT License. See [LICENSE.txt](LICENSE.txt).
