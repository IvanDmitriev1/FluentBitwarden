# FluentBitwarden.Ui

## Scope and role

Root instructions apply. This WinUI 3 process owns windows, pages, dialogs, and presentation state; all business state comes from AppHost IPC, so it can be closed and relaunched without changing the unlocked session.

## Project structure

```text
FluentBitwarden.Ui/
├── Program.cs                 # Entry point, host startup
├── App.xaml(.cs)              # XAML resources
├── Screens/                   # Top-level application content
│   ├── Shell/                 # Unlocked layout and Frame
│   ├── Setup/                 # Sign-in and confirmation
│   ├── Unlock/                # Vault unlocking
│   └── Loading/               # Loading and readiness presentation
├── Infrastructure/            # Services, helpers, and extensions
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
- Keep code-behind limited to initialization and WinUI-specific behavior.
- Keep view models independent of `Window`, `Page`, `Frame`, and visual controls.
- Keep feature-specific code inside its feature directory. Move code to shared folders only when genuinely reused.
- Keep authoritative state in AppHost. UI holds disposable presentation state only.
- Use asynchronous IPC calls with cancellation. Never block the UI thread.
- Organize UI code by feature. Keep views, view models, and feature-specific controls together. Use shared folders only for cross-feature components. Introduce subfolders when they improve navigation; avoid empty or single-file organizational layers. Keep AppHost state behind typed IPC clients.

## Shell and navigation

- `MainWindow` hosts `SetupView` or `UnlockView` or `ShellView` based on AppHost session state.
- `ShellView` owns persistent layout and a `Frame` for feature navigation.
- Each navigable feature has one `FeatureNamePage.xaml` directly in its feature directory.
- Place feature-specific reusable views in `Features/<Feature>/Views/`.
- Use `INavigationService` for navigation. Do not navigate directly from view models.
- Clear navigation history and sensitive presentation state when AppHost locks.
- Rebuild UI state from AppHost after restart or reconnection.

## Windows and dialogs

- `MainWindow`: single primary application window.
- `CompactWindow`: generic, non-resizable window for independent content.
- `OverlayWindow`: standalone window for prompts when needed.
- `IWindowManager` owns window creation, tracking, visibility, and closing.
- `IUserDialogPresenter` presents reusable dialogs inside `MainWindow` or `OverlayWindow`.
- Dialog content must not depend on its presentation location.
- Keep dialogs and independent windows outside shell navigation.
- Dispatch all WinUI operations to the UI thread.
- Dispose subscriptions and cancel pending operations when views close.

## XAML and styling

- Always use compiled `{x:Bind}` over runtime `{Binding}`!
- Specify binding modes explicitly: OneTime, OneWay, or TwoWay.
- Use strongly typed view models and explicit binding modes.
- Use `x:DataType` for compiled bindings in `DataTemplate`.
- Prefer commands over event handlers for application actions.
- Use `ThemeResource` and shared styles instead of hardcoded colors and dimensions.
- Put reusable styles in `Styles/`; default templated-control styles in `Themes/Generic.xaml`.
- Use `UserControl` for composed UI and templated controls when customizable templates are required.
- Preserve keyboard navigation, accessibility, localization, and light/dark themes.

## Implementation rules

- Read relevant feature files and nearby patterns before editing.
- Make smallest coherent change at correct ownership boundary.
- Avoid duplicate services, unnecessary abstractions, and speculative scaffolding.
- Do not edit generated files.
- Handle IPC disconnects, cancellation, loading, and failures.
- Never log or persist secrets in presentation state.
- Test view-model logic without UI dependencies.
- Build and verify XAML after changes.
- Follow existing conventions unless change explicitly requires restructuring.
