# FluentBitwarden BrowserHost

`FluentBitwarden.BrowserHost` is the native messaging host used by browser extensions. It is a bridge only: it reads browser Native Messaging JSON requests from stdin, forwards supported requests to AppHost through the existing named-pipe IPC contracts, and writes JSON responses to stdout.

Native Messaging frames use a 4-byte unsigned little-endian length prefix followed by a UTF-8 JSON payload. BrowserHost keeps reading frames until stdin closes, then exits cleanly.

Stdout is reserved for Native Messaging protocol frames. Do not write logs, diagnostics, or debug text to stdout. AppHost must be running for browser requests to succeed.

BrowserHost targets Windows only for now. Manifest templates are in `manifests/`:

- `chrome.windows.json`
- `firefox.windows.json`
