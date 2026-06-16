# FluentBitwarden BrowserHost

`FluentBitwarden.BrowserHost` is the native messaging host used by browser extensions. It is a bridge only: it reads browser Native Messaging JSON requests from stdin, forwards supported requests to AppHost through the existing named-pipe IPC contracts, and writes JSON responses to stdout.

Stdout is reserved for Native Messaging protocol.
