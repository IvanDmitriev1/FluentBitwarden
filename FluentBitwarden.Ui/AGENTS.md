# FluentBitwarden.Ui

## Scope and role

Root instructions apply. This WinUI 3 process owns windows, pages, dialogs, and presentation state; all business state comes from AppHost IPC, so it can be closed and relaunched without changing the unlocked session. Its root namespace is FluentBitwarden, not FluentBitwarden.Ui.

## Local map and MVVM

- App.xaml.cs and Program.cs: composition, activation, and exception wiring.
- Application/: app-level window and session coordination.
- Views/: XAML pages and [ViewRegistration.cs](Views/ViewRegistration.cs).
- ViewModels/: feature view models and navigation intents.
- Infrastructure/: remote clients, navigation, dialogs, converters, validation, and window helpers.
- Controls/, Templates/, Styles/, and AttachedProperties/: reusable presentation infrastructure.

View models are sealed partial, derive from ObservableObject, use primary-constructor injection, and use CommunityToolkit source-generated [ObservableProperty] and [RelayCommand]. Pages have parameterless constructors because WinUI creates them; resolve only their view model from App.Current, assign DataContext, and keep all other behavior out of code-behind. Register view models, never pages.

Pages derive from LifecyclePage. Implement IPageLifecycleAware or IPageLifecycleAware<TIntent> and honor the supplied cancellation token; navigation can unload the page while it is loading. Keep intents small records in the feature's ViewModels/<Feature>/Models/ directory.

## Integration and XAML

Use only IXxxClient interfaces from Contracts; Infrastructure/Clients/Remote* implementations remain thin IIpcClient pass-throughs. Marshal event callbacks to App.Current.DispatcherQueue. Never reference AppHost.

Use x:Bind for new XAML where possible. Use {ThemeResource} rather than hard-coded page colors, put reusable styles/templates/controls in their matching folders, and put XAML-only behavior in AttachedProperties/. Custom controls use DependencyPropertyGenerator and PART_ constants.

## Dialog hosting

Feature dialogs are fresh XAML-backed ContentDialog types under Views/UserDialogs/. Construct them on the UI thread and present them only through IUiDialogCoordinator, which serializes presentation, assigns the active window XamlRoot, handles cancellation, and closes the overlay after completion. Dialogs implementing IUserDialog<TResult> own their typed result; ordinary ContentDialog instances return ContentDialogResult. Both window modes use popup placement.

## Verification and completion

Run the repository CI build for UI or contract changes. There are no UI test projects in the repository. Verify lifecycle cancellation, correct view-model registration, and the relevant theme/accessibility behavior.
