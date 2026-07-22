# AGENTS.md — FluentBitwarden.Ui

The WinUI 3 presentation process. It owns windows, pages, and dialogs, and holds **no** business state:
everything it shows comes from the AppHost over IPC. It can be closed and relaunched at any time without
disturbing the unlocked session.

Read [the root guide](../AGENTS.md) first, especially Recipe C (add a page).

## Namespace gotcha

`RootNamespace` is `FluentBitwarden`, not `FluentBitwarden.Ui`. A file in `Infrastructure/Clients/`
belongs to `FluentBitwarden.Infrastructure.Clients`. Follow the folder, drop the `Ui`.

## Layout

```
App.xaml.cs / Program.cs   composition root: DI container, activation, exception wiring
Application/               app coordination: window/session orchestration, hosted services
Views/                     pages (XAML + thin code-behind) and ViewRegistration.cs
ViewModels/                one folder per feature, mirrors Views/
Controls/                  reusable templated controls
Templates/ Styles/         XAML resource dictionaries
AttachedProperties/        attached behaviours (XAML-only wiring)
Infrastructure/            clients, navigation, converters, dialogs, validation, window helpers
```

## MVVM

CommunityToolkit.Mvvm, source-generated. View models are `sealed partial`, derive `ObservableObject`,
and take their dependencies through a primary constructor:

```csharp
public sealed partial class FooPageViewModel(IFooClient fooClient) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    public partial Foo? SelectedFoo { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    partial void OnSelectedFooChanged(Foo? value) { /* react to selection */ }

    [RelayCommand]
    private async Task SaveFoo(CancellationToken cancellationToken)
    {
        var saved = await fooClient.SaveFooAsync(new SaveFooRequest(SelectedFoo!), cancellationToken);
        if (saved is null)
            return;   // save failed; stay put so the user can retry

        SelectedFoo = saved;
    }
}
```

Note the `partial` property form of `[ObservableProperty]` — that is the style used throughout; do not
add backing fields by hand. `[RelayCommand]` on an async method with a `CancellationToken` parameter
gives you cancellation for free.

Code-behind is a parameterless constructor + `ViewModel` property, nothing else:

```csharp
public sealed partial class FooPage : LifecyclePage
{
    public FooPage()
    {
        ViewModel = App.Current.GetRequiredService<FooPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public FooPageViewModel ViewModel { get; }
}
```

The constructor takes no parameters because WinUI instantiates the page itself inside
`Frame.Navigate(pageType)` — there is no hook for constructor injection, so the page resolves its view
model from the container instead. That service-locator call is confined to page constructors; everywhere
else uses constructor injection.

Logic that is tempting to put in code-behind belongs in the view model, an attached behaviour, or a
control.

## Page lifecycle

Pages derive [`LifecyclePage`](Infrastructure/Navigation/LifecyclePage.cs), which reconciles WinUI's
`OnNavigatedTo` with `Loaded`, creates a `CancellationTokenSource` per load, cancels it on navigate-away
or unload, and logs failures instead of crashing on `async void`.

The view model opts in through one of:

| Interface | Use when |
| --- | --- |
| `IPageLifecycleAware` | Plain navigation, no payload. `OnLoadingAsync(ct)`. |
| `IPageLifecycleAware<TIntent>` | Navigation carries a payload. `OnLoadingAsync(intent, ct)`. |

Intents are small records in `ViewModels/<Feature>/Models/`, e.g.
`public sealed record OpenVaultCipherIntent(CipherId CipherId);`. A view model can implement several
intent overloads — see [VaultPageViewModel.cs](ViewModels/Vault/VaultPageViewModel.cs).

Always honour the `CancellationToken`: the page may be navigated away from mid-load.

## Talking to the AppHost

Only through the `IXxxClient` interfaces from `FluentBitwarden.Contracts`. The `Remote*` implementations
in [Infrastructure/Clients](Infrastructure/Clients) are pass-throughs over `IIpcClient` and must stay
that way — no business rules, no caching decisions beyond the obvious (the site-icon preload in
`RemoteVaultClient` is the one deliberate extra, and it is fire-and-forget).

For push notifications from the host, subscribe with `IIpcEventClient` and marshal to the UI thread via
`App.Current.DispatcherQueue`.

Never reference `FluentBitwarden.AppHost` — there is no project reference and there never will be.

## Registration

- View models: `services.AddTransient<FooPageViewModel>();` in
  [Views/ViewRegistration.cs](Views/ViewRegistration.cs). **Pages are not registered** — WinUI creates
  them, not the container.
- Services and remote clients: [Infrastructure/ServiceCollectionExtensions.cs](Infrastructure/ServiceCollectionExtensions.cs).
- App-level coordination: `Application/`.

The container is built with validation in `DEBUG`, so a view model with an unresolvable dependency fails
at startup rather than on first navigation.

## XAML and controls

- Root element of a page is `navigation:LifecyclePage`, with `NavigationCacheMode="Required"` when the
  page should survive back-navigation.
- Brushes and colours come from `{ThemeResource}` so light/dark/high-contrast keep working. No hard-coded
  hex colours in page XAML.
- Shared styles live in `Styles/`, item templates in `Templates/`, reusable controls in `Controls/`.
- Custom controls use `DependencyPropertyGenerator` attributes rather than hand-written
  `DependencyProperty.Register` boilerplate, and name template parts with `PART_` constants:

```csharp
[TemplatePart(Name = PartChrome, Type = typeof(FooChrome))]
[DependencyProperty<string>("Label", DefaultValue = "")]
[DependencyProperty<string>("Text")]
public partial class FooField : Control
{
    private const string PartChrome = "PART_Chrome";

    public FooField() => DefaultStyleKey = typeof(FooField);

    protected override void OnApplyTemplate() { /* grab template children, wire events */ }
}
```

- XAML-only wiring (focus, selection sync, edit behaviours) goes into `AttachedProperties/` rather than
  code-behind.
- Prefer `x:Bind` over `Binding` in new XAML; it is compiled, faster, and fails loudly.

## Global usings

[GlobalUsings.cs](GlobalUsings.cs) carries the namespaces used across most view models and pages. Add
widely-shared namespaces there instead of repeating them file by file; keep one-off namespaces local.
