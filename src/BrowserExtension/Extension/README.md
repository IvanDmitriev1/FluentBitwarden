# FluentBitwarden BrowserExtension

Manifest V3 browser extension for FluentBitwarden browser integration.

The extension detects login and OTP fields on web pages, asks the background service worker whether matching credentials are available, shows a small inline `FB` button, and fills credentials only after the user clicks it.

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
