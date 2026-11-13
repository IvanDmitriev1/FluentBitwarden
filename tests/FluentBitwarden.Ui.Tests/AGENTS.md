# FluentBitwarden.Ui.Tests

Follow the [repository guide](../../AGENTS.md) and the [UI conventions](../../src/FluentBitwarden.Ui/AGENTS.md). This project uses xUnit v3 and NSubstitute. Keep scenario tests next to their feature under the matching test directory. Use the [setup tests](Screens/Setup) as the starting point.

## Choose the test boundary

- Test view-model commands and state without starting WinUI. Give each case fresh substitutes for the dependencies it exercises. Assert observable state and contractual calls.
- Use desktop tests for behavior that crosses a real XAML boundary. Keep navigation, controls, and bindings real. Replace IPC and package-dependent settings in [UiTestComposition](UiTestComposition.cs) with NSubstitute and synthetic data.

## Reuse desktop infrastructure

- Put window-driving tests in the nonparallel `DesktopUiCollection`. Reuse `DesktopUiFixture` for the shared WinUI host and window-scoped automation. Arrange each scenario's starting state explicitly. Keep scenario actions and assertions in the feature tests.
- Read [TestsInfrastructure](TestsInfrastructure) before adding helpers. Use `UiSelectors.ById` with a root for shared template controls. Wait for the specific element and observable state, then act on the matched element. For visible feedback, assert both the expected text and visibility.
- Use bounded waits with the xUnit cancellation token. Keep window and dispatcher access in the fixture. Keep UI Automation calls on the test worker and target the fixture window. The project uses the WinApp UI Automation NuGet API directly.
- Keep fixtures and helpers focused on product scenarios. Add shared stateless helpers when there is an actual second caller. Keep synthetic credentials in `TestCredentials` and use no real account or vault data.

## Desktop test preconditions

- Start each desktop scenario through a fixture method that establishes its starting screen and waits until it is ready for interaction. For sign-in tests, readiness means the email step is visible and its input is enabled, even when the session is already signed out. If readiness fails, report the expected screen and the page the window actually hosts.
- When re-entering a screen with a nested navigation frame, wait for the old frame to detach before navigating back. Keep that wait in the fixture.
- Select template parts by stable automation ID scoped to the owning control. A type-only selector can match multiple buttons or inputs in the same template.
- Check whether a view-model test can cover a new scenario first. Reserve desktop tests for behavior that depends on real XAML, bindings, templates, or navigation.

## Run the suite

Build and run from Visual Studio Test Explorer with x64 selected, or run from the repository root on an interactive x64 Windows desktop with the matching Windows App Runtime:

```powershell
dotnet test --project tests/FluentBitwarden.Ui.Tests/FluentBitwarden.Ui.Tests.csproj -c Release -p:Platform=x64
```

Report the cases exercised and any desktop checks the environment prevented.
