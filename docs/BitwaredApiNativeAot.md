# `BitwaredApi` Native AOT

## What this means for a library

`BitwaredApi` is a class library, not an executable. Native AOT enablement for this project means:

- the library is safe to consume from Native AOT applications
- trimming is treated as part of the library contract
- platform-specific native publish validation is done explicitly, not by forcing every build to use `PublishAot`

The project intentionally keeps `IsAotCompatible` and `IsTrimmable` enabled in the project file, but does not set `PublishAot`, `PublishTrimmed`, or `TrimMode` globally.

Reference guidance:

- [Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Native AOT libraries](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/libraries)

## Project contract

- `BitwaredApi` stays a class library.
- JSON serialization must continue to use the source-generated `BitwaredApiJsonContext`.
- Do not introduce runtime reflection, dynamic code generation, or platform-specific APIs into `BitwaredApi`.
- Closed generic DI registrations in `BitwaredApiServiceCollectionExtensions` are acceptable.
- `ConfigureAwait.Fody` remains in place unless it starts producing real trim or AOT warnings in publish validation.

## Local validation commands

Run these from the repo root:

```powershell
dotnet build .\BitwaredApi\BitwaredApi.csproj
dotnet build .\BitwaredApi\BitwaredApi.csproj -p:IsAotCompatible=true -p:IsTrimmable=true -warnaserror
```

Validate Native AOT publish on a matching host OS:

```powershell
dotnet publish .\BitwaredApi\BitwaredApi.csproj -c Release -r win-x64 -p:PublishAot=true
```

```bash
dotnet publish ./BitwaredApi/BitwaredApi.csproj -c Release -r linux-x64 -p:PublishAot=true
dotnet publish ./BitwaredApi/BitwaredApi.csproj -c Release -r osx-arm64 -p:PublishAot=true
```

Cross-OS native compilation is not supported. Build Windows artifacts on Windows, Linux artifacts on Linux, and macOS artifacts on macOS.

## Expected output

Native publish output is written under:

```text
BitwaredApi/bin/<Configuration>/net10.0/<RID>/
```

The exact files vary by OS. Typical results include:

- `publish/` containing the Native AOT-managed library output
- on Windows, a `native/` directory with native import library artifacts such as `.lib` and `.exp`

## Smoke verification

After publish succeeds on a matching host OS:

- confirm there are zero trim or AOT warnings
- confirm native artifacts were emitted for that RID
- confirm a consuming app or local harness can resolve DI registrations
- execute at least one auth JSON path and one vault decrypt path
