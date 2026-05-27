# Toolkit migration status (issue #32)

Tracks adoption of `MZikmund.Toolkit.WinUI` promotions. As each type ships in the toolkit, the
template deletes its own copy, references the toolkit type, and updates DI registration. The goal:
~80% of the template's files become NuGet references, leaving only the ~20% that legitimately
differ per app.

Referenced toolkit version: **`MZikmund.Toolkit.WinUI` 0.1.13-dev.43** (see
`src/Directory.Packages.props`).

Status legend: **Done** = swapped to toolkit type · **Pending** = blocked on not-yet-shipped
toolkit work (template copy intentionally retained).

## §18.1 — duplicates already in toolkit

| Item | Toolkit type | Status | Notes |
| --- | --- | --- | --- |
| `Services/Dialogs/IDialogCoordinator.cs` | `MZikmund.Toolkit.WinUI.Services.IDialogCoordinator` | **Done** | Deleted. Surface identical (`Task<ContentDialogResult> ShowAsync(ContentDialog)`). |
| `Services/Dialogs/DialogCoordinator.cs` | `MZikmund.Toolkit.WinUI.Services.DialogCoordinator` | **Done** | Deleted. Toolkit version implements `IDialogCoordinator` with a parameterless ctor and additionally validates `XamlRoot` is set. |
| `Services/Dialogs/QueuedDialog` (private nested) | inside toolkit `DialogCoordinator` | **Done** | Removed with `DialogCoordinator.cs` (was a private nested class). |
| `Services/Navigation/IXamlRootProvider.cs` | `MZikmund.Toolkit.WinUI.Infrastructure.IXamlRootProvider` | **Done** | Deleted. Surface identical (`XamlRoot XamlRoot { get; }`). `WindowShellProvider` now implements the toolkit interface. |
| `Services/Settings/IPreferences.cs` | `MZikmund.Toolkit.WinUI.Services.IPreferences` | **Pending** | Toolkit interface is **not** a superset: it has `Get`/`TryGet`/`Set`/`GetComplex`/`TryGetComplex`/`SetComplex` but **lacks** `ContainsKey`/`Remove`/`Clear`, which the template's public `IPreferences` declares. Blocked on toolkit issue "Extend IPreferences with TryGet/ContainsKey/Remove/Clear". |
| `Services/Settings/Preferences.cs` | `MZikmund.Toolkit.WinUI.Services.Preferences` | **Pending** | Tied to `IPreferences` above. Retained until the toolkit interface gains the missing members. |

### How the swaps are wired

Because the template still ships its own `IPreferences`/`Preferences` (in
`AppTemplate.Services.Settings`), importing the whole `MZikmund.Toolkit.WinUI.Services` namespace
globally would collide with them. To keep the swap clean, `src/AppTemplate/GlobalUsings.cs`:

- imports `MZikmund.Toolkit.WinUI.Infrastructure` globally (only `IXamlRootProvider` lives there), and
- adds global using **aliases** for the dialog coordinator types:

  ```csharp
  global using IDialogCoordinator = MZikmund.Toolkit.WinUI.Services.IDialogCoordinator;
  global using DialogCoordinator = MZikmund.Toolkit.WinUI.Services.DialogCoordinator;
  ```

All existing references (`App.xaml.cs` DI registration, `DialogService`, `ConfirmationDialogService`,
`WindowShellProvider`) compile unchanged against the toolkit types.

## §18.2 — promotions awaiting toolkit issues

All of the following remain in the template and are **Pending** the corresponding toolkit promotion
issue. None were forced.

| Item | Toolkit issue | Status |
| --- | --- | --- |
| Dialogs: `IDialogService`, `DialogService`, `IConfirmationDialogService`, `ConfirmationDialogService`, `ConfirmationResult` | Promote dialog services | **Pending** |
| Platform services: `IDisplayRequestManager`/`DisplayRequestManager`, `ILauncherService`/`LauncherService`, `IShareService`/`ShareService` | Promote platform services | **Pending** |
| Converters: `NullToVisibilityConverter`, `EnumLocalizationConverter` | Promote XAML converters | **Pending** |
| Markup + Localization: `Markup/LocalizeExtension.cs`, `Services/Localization/Localizer.cs` | Promote LocalizeExtension and Localizer | **Pending** |
| HTTP: `Services/Http/DebugHttpHandler.cs` | Promote DebugHttpHandler | **Pending** |
| Theming: `IThemeManager`, `ThemeManager.cs` | Promote IThemeManager + ThemeManager | **Pending** |
| Rating: `IAppRatingService`, `AppRatingService.cs` (supply store IDs via `IOptions<AppRatingOptions>`) | Promote IAppRatingService + AppRatingService | **Pending** |
| Shell + window infrastructure: `Infrastructure/IWindowShell.cs`, `Services/Navigation/IWindowShellProvider.cs`, `Services/Navigation/WindowShellProvider.cs` | Promote shell + window infrastructure | **Pending** |
| Navigation (with `NavigationSection` → string-tags refactor): `INavigationService`, `NavigationService`, `NavigationInfoAttribute`, `NavigationTransition` | Promote navigation infrastructure | **Pending** |
| Core infrastructure: `IoC`, `IAppUpdater` (interface only), `ViewModelBase`, `WindowShellViewModel` | Promote Core infrastructure | **Pending** |
| `Views/ViewBase.cs` | Promote ViewBase<TViewModel> | **Pending** |

Catalog reference: ENHANCEMENTS.md §18 (entire section).
