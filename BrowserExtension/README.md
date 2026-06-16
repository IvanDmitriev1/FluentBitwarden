# FluentBitwarden BrowserExtension

Manifest V3 browser extension for FluentBitwarden browser integration.

The extension detects login and OTP fields on web pages, asks the background service worker whether matching credentials are available, shows a small inline `FB` button, and fills credentials only after the user clicks it.

## Project Structure

- `src/background`: extension service worker, native messaging client, and credential availability cache.
- `src/content`: page field detection, inline autofill UI, scan coordination, and fill logic.
- `src/shared`: extension message contracts and native browser protocol types.
- `vite.config.ts`: browser-specific Manifest V3 generation and build entry points.
- `dist/chrome`: Chromium extension build output.
- `dist/firefox`: Firefox extension build output.

## Native Messaging

The extension talks to FluentBitwarden through the native host id `com.fluentbitwarden.browseproxy`.

Native host registration is handled by the main FluentBitwarden app through `BrowserExtensionSetupService`. The native process is `FluentBitwarden.BrowseProxy`.

## Build

Build the Chromium extension:

```powershell
pnpm run build:chrome
```

Build the Firefox extension:

```powershell
pnpm run build:firefox
```
