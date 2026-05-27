# Shared toolkit adoption

This app builds on `MZikmund.Toolkit.WinUI`, a shared library of cross-app WinUI/Uno
infrastructure. Wherever the toolkit ships a type that matches what the app needs, the app
references the toolkit type instead of carrying a local copy; everything else stays local because it
is genuinely app-specific or not yet covered by the toolkit.

Referenced toolkit version: **`MZikmund.Toolkit.WinUI` 0.1.19-dev.65** (see
`src/Directory.Packages.props`).

Status legend: **Adopted** = the app references the toolkit type · **Local** = an app copy is kept,
with the reason given.

## Adopted from the toolkit

| Former local file | Toolkit type | Notes |
| --- | --- | --- |
| `Services/Dialogs/IDialogCoordinator.cs` | `MZikmund.Toolkit.WinUI.Services.IDialogCoordinator` | Identical surface (`Task<ContentDialogResult> ShowAsync(ContentDialog)`). |
| `Services/Dialogs/DialogCoordinator.cs` | `MZikmund.Toolkit.WinUI.Services.DialogCoordinator` | Implements `IDialogCoordinator` with a parameterless ctor and additionally validates that `XamlRoot` is set. The former local copy queued dialogs via a private nested `QueuedDialog`; the toolkit version covers the same behaviour. |
| `Services/Navigation/IXamlRootProvider.cs` | `MZikmund.Toolkit.WinUI.Infrastructure.IXamlRootProvider` | Identical surface (`XamlRoot XamlRoot { get; }`). `WindowShellProvider` implements the toolkit interface. |
| `Services/Settings/IPreferences.cs` | `MZikmund.Toolkit.WinUI.Services.IPreferences` | The toolkit interface is now a superset of the app's usage: `Get`/`Set`/`GetComplex`/`SetComplex`/`ContainsKey`/`Remove`/`Clear` (plus `TryGet`/`TryGetComplex`). Adopted after the toolkit added `ContainsKey`/`Remove`/`Clear` in `0.1.19-dev.65`. |
| `Services/Settings/Preferences.cs` | `MZikmund.Toolkit.WinUI.Services.Preferences` | `ApplicationData.LocalSettings`-backed implementation; behaviour matches the former local copy. `AppPreferences`/`IAppPreferences` stay local — they map app-specific keys onto `IPreferences`. |

### How the references are wired

`src/AppTemplate/GlobalUsings.cs`:

- imports `MZikmund.Toolkit.WinUI.Infrastructure` globally (only `IXamlRootProvider` lives there), and
- adds global using **aliases** for the consumed `Services` types:

  ```csharp
  global using IDialogCoordinator = MZikmund.Toolkit.WinUI.Services.IDialogCoordinator;
  global using DialogCoordinator = MZikmund.Toolkit.WinUI.Services.DialogCoordinator;
  global using IPreferences = MZikmund.Toolkit.WinUI.Services.IPreferences;
  global using Preferences = MZikmund.Toolkit.WinUI.Services.Preferences;
  ```

Aliases are used instead of importing the whole `MZikmund.Toolkit.WinUI.Services` namespace because
that namespace also defines `IAppRatingService`/`AppRatingService`, which would collide with the
app's own rating types of the same name (see below). DI registration in `App.xaml.cs`, `DialogService`,
`ConfirmationDialogService`, and `WindowShellProvider` all compile unchanged against the toolkit types.

## Kept local (by design)

These remain in the app. Some are app-specific by nature; others are candidates for a future toolkit
release but are not covered today.

| Item | Reason |
| --- | --- |
| Rating: `Services/Rating/IAppRatingService.cs`, `AppRatingService.cs` | Intentionally local. The app's contract (`TryPromptForRatingAsync()`) orchestrates a localized confirmation dialog and store-review launch. The toolkit's same-named `IAppRatingService` is a different abstraction (a launch-count tracker: `IncrementLaunchCount`/`RequestRatingAsync`/`ShouldRequestRating` configured via `AppRatingOptions`), so it is not a drop-in replacement. |
| Theming: `Services/Theming/IThemeManager.cs`, `ThemeManager.cs` | Not in the toolkit. |
| Dialogs: `IDialogService`, `DialogService`, `IConfirmationDialogService`, `ConfirmationDialogService`, `ConfirmationResult` | Not in the toolkit. |
| Platform services: `IDisplayRequestManager`/`DisplayRequestManager`, `ILauncherService`/`LauncherService`, `IShareService`/`ShareService` | Not in the toolkit. |
| Converters: `NullToVisibilityConverter`, `EnumLocalizationConverter` | Not in the toolkit. |
| Markup + Localization: `Markup/LocalizeExtension.cs`, `Services/Localization/Localizer.cs` | Not in the toolkit. |
| HTTP: `Services/Http/DebugHttpHandler.cs` | Not in the toolkit. |
| Shell + window infrastructure: `Infrastructure/IWindowShell.cs`, `Services/Navigation/IWindowShellProvider.cs`, `Services/Navigation/WindowShellProvider.cs` | Not in the toolkit. `WindowShellProvider` additionally implements the toolkit `IXamlRootProvider`. |
| Navigation: `INavigationService`, `NavigationService`, `NavigationInfoAttribute`, `NavigationTransition` | Not in the toolkit. |
| Core infrastructure: `IoC`, `IAppUpdater`, `ViewModelBase`, `WindowShellViewModel` | Not in the toolkit. |
| `Views/ViewBase.cs` | Not in the toolkit. |
