# COM server native tests

These tests use the Visual Studio C++ native unit test framework. The test project references the COM server project and asks it for its compiled object inputs, excluding `WinMain`, so production objects stay aligned with the selected configuration and architecture.

## Structure

- `Ipc/` contains protocol header and binary payload tests.
- `Cbor/` contains direct CBOR writer tests, including literal boundary bytes.
- `Passkey/` contains direct passkey message tests and the authenticator GetInfo builder fixture.
- `Support/` contains per-test doubles for the passkey client and AppHost launcher.

The authenticator calls the real namespace verifier. Its tests cover pointer checks and malformed-request rejection before client calls or response writes. Decoder and encoder tests exercise payload handling separately. Successful signed requests through the authenticator require Windows-issued signatures and are not covered by this unit suite.


## Build and run

Run these commands from the repository root in a Visual Studio Developer PowerShell. The x64 commands build and run the native suite in Debug and Release; ARM64 is compiled in Release as a platform check.

```powershell
msbuild tests\FluentBitwarden.ComServer.Tests\FluentBitwarden.ComServer.Tests.vcxproj /p:Configuration=Debug /p:Platform=x64 /v:minimal
vstest.console.exe tests\FluentBitwarden.ComServer.Tests\bin\x64\Debug\FluentBitwarden.ComServer.Tests.dll /Platform:x64 /Logger:trx /ResultsDirectory:TestResults\ComServerNativeDebug -- RunConfiguration.TreatNoTestsAsError=true
msbuild tests\FluentBitwarden.ComServer.Tests\FluentBitwarden.ComServer.Tests.vcxproj /p:Configuration=Release /p:Platform=x64 /v:minimal
vstest.console.exe tests\FluentBitwarden.ComServer.Tests\bin\x64\Release\FluentBitwarden.ComServer.Tests.dll /Platform:x64 /Logger:trx /ResultsDirectory:TestResults\ComServerNativeRelease -- RunConfiguration.TreatNoTestsAsError=true
msbuild tests\FluentBitwarden.ComServer.Tests\FluentBitwarden.ComServer.Tests.vcxproj /p:Configuration=Release /p:Platform=ARM64 /v:minimal
```

## Coverage

Rebuild the native tests in Debug x64, then collect coverage with the checked-in runsettings file:

```powershell
msbuild tests\FluentBitwarden.ComServer.Tests\FluentBitwarden.ComServer.Tests.vcxproj /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /v:minimal
vstest.console.exe tests\FluentBitwarden.ComServer.Tests\bin\x64\Debug\FluentBitwarden.ComServer.Tests.dll /Platform:x64 /EnableCodeCoverage /Settings:tests\FluentBitwarden.ComServer.Tests\coverage.runsettings /Logger:trx /ResultsDirectory:TestResults\ComServerNativeCoverage -- RunConfiguration.TreatNoTestsAsError=true
```
