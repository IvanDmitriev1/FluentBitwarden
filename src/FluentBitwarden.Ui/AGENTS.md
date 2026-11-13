# FluentBitwarden.Ui

## Scope and role

Follow the [repository guide](../../AGENTS.md). This WinUI 3 process owns windows, pages, dialogs, and presentation state. AppHost owns vault and session state and exposes it through typed IPC contracts.

## Project structure

```text
FluentBitwarden.Ui/
├── Program.cs                 # Entry point, host startup
├── App.xaml(.cs)              # XAML resources
├── Application/               # Activation routing and session monitoring
├── Screens/                   # Top-level application content
│   ├── Shell/                 # Unlocked layout and Frame
│   ├── Setup/                 # Sign-in and confirmation
│   ├── Unlock/                # Vault unlocking
│   └── Loading/               # Loading and readiness presentation
├── Infrastructure/            # Hosting, UI registration, and remote IPC clients
├── Windows/                   # Native Main and Overlay hosts
├── Features/                  # Feature pages displayed inside Shell
├── Dialogs/                   # Dialog service, hosts, views
├── Controls/                  # Shared reusable controls
├── Styles/                    # Resource dictionaries
├── Themes/                    # Custom control templates
├── Converters/                # Binding converters
├── Assets/                    # Images, icons
└── Strings/                   # Localization
```

Structure is the intended convention. Inspect existing files before creating or moving anything.

Keep each screen's view, code-behind, view model, and screen-specific controls together under `Screens/<Screen>/`. Put native window hosts in `Windows/`, feature pages in `Features/`, and prompt content in `Dialogs/`.

## Architecture and MVVM

- Follow MVVM. Views handle presentation; view models handle presentation state and commands.
- Use `CommunityToolkit.Mvvm` with `[ObservableProperty]` and `[RelayCommand]`.
- Use constructor injection through .NET Generic Host.
- Register shared presentation services in [AddUiServices](Infrastructure/UiServiceCollectionExtensions.cs). Keep production IPC and platform registrations in [Program.cs](Program.cs). Consume settings through `ISettingsStore`, which `AddPlatformServices` registers.
- Keep code-behind limited to initialization and WinUI-specific behavior.
- Keep view models independent of `Window`, `Page`, `Frame`, and visual controls.
- Keep feature-specific code beside its consumers. Put reusable stateless methods in focused static classes when they have multiple callers.
- Use asynchronous IPC calls with cancellation. Never block the UI thread.

## Shell and navigation

- [AppRouter](Application/AppRouter.cs) routes session state to Loading, Setup, Unlock, or Shell through `IWindowManager`. [AppSessionMonitor](Application/AppSessionMonitor.cs) refreshes the snapshot and handles IPC events.
- `ShellPage` owns persistent layout and a `Frame` for feature navigation.
- Each navigable feature has one `FeatureNamePage.xaml` directly in its feature directory.
- Place feature-specific reusable views in `Features/<Feature>/Views/`.
- Use the existing Navis `INavigation` or feature navigator. Keep direct `Frame` operations in the view and hosting layers.
- Clear navigation history and sensitive presentation state when AppHost locks.
- Rebuild UI state from AppHost after restart or reconnection.

## Windows and dialogs

- `MainWindow`: single primary application window.
- `CompactWindow`: generic, non-resizable window for independent content.
- `OverlayWindow`: standalone window for prompts when needed.
- `IWindowManager` owns window creation, tracking, visibility, and closing.
- Dialog content must not depend on its presentation location.
- Keep dialogs and independent windows outside shell navigation.
- Dispatch all WinUI operations to the UI thread.
- Reuse [WinUiThread](Infrastructure/Hosting/WinUiThread.cs) for the STA and XAML loop. Release dispatcher-affine services before exiting the application.
- Dispose subscriptions and cancel pending operations when views close.

## XAML and styling

- Use compiled `{x:Bind}` with explicit `OneWay`, or `TwoWay` modes and strongly typed view models.
- Use `x:DataType` for compiled bindings in `DataTemplate`.
- Prefer commands over event handlers for application actions.
- Use `ThemeResource` and shared styles instead of hardcoded colors and dimensions.
- Put reusable styles in `Styles/`; default templated-control styles in `Themes/Generic.xaml`.
- Use `UserControl` for composed UI and templated controls when customizable templates are required.
- Preserve keyboard navigation, accessibility, localization, and light/dark themes.
- Give scenario controls stable automation IDs. Use a generic ID for shared template parts and scope the test selector under the relevant control instance.

## Implementation rules

- Read relevant feature files and nearby patterns before editing.
- Make smallest coherent change at correct ownership boundary.
- Avoid duplicate services, unnecessary abstractions, and speculative scaffolding.
- Do not edit generated files.
- Handle IPC disconnects, cancellation, loading, and failures.
- Never log or persist secrets in presentation state.
- Follow existing conventions unless change explicitly requires restructuring.

## Verification

- Build the affected WinUI project after XAML or hosting changes. Follow the repository build guidance for shared or cross-project changes.
- Use view-model tests for command and state logic. Use desktop tests for behavior that depends on real controls, bindings, templates, or navigation. Follow the [UI test guide](../../tests/FluentBitwarden.Ui.Tests/AGENTS.md).
