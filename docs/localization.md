# Localization

A cross-platform [Uno Platform](https://platform.uno/) (WinUI) application targeting .NET 10.

Localized strings come from Uno's `.UseLocalization()` host extension (configured in
[`src/AppTemplate/App.xaml.cs`](../src/AppTemplate/App.xaml.cs)), which registers a
`Microsoft.Extensions.Localization.IStringLocalizer` backed by the `.resw` files under
[`src/AppTemplate/Strings/`](../src/AppTemplate/Strings/).

There are two ways the app resolves that `IStringLocalizer`:

- **Dependency injection**, wherever constructor injection is available (ViewModels,
  `WindowShell`). These get an `IStringLocalizer` directly from the container.
- **The static [`Localizer`](../src/AppTemplate/Services/Localization/Localizer.cs) accessor**,
  for the contexts that *cannot* use constructor injection — XAML markup extensions, value
  converters, and services that build dialogs ad hoc. `Localizer.Instance` lazily resolves the
  *same* `IStringLocalizer` from the container via the `IoC` service locator
  ([`src/AppTemplate.Core/Infrastructure/IoC.cs`](../src/AppTemplate.Core/Infrastructure/IoC.cs))
  and returns `???{key}???` for missing keys.

Both paths read from the same `IStringLocalizer`, so there is a single source of localized
strings regardless of how a given call site reaches it.

| Consumer | File | How it obtains strings |
| --- | --- | --- |
| `MainViewModel`, `SettingsViewModel` | `src/AppTemplate/ViewModels/` | Constructor-injected `IStringLocalizer` (DI) |
| `WindowShell` | `src/AppTemplate/WindowShell.xaml.cs` | `ServiceProvider.GetRequiredService<IStringLocalizer>()` |
| `LocalizeExtension` (XAML markup) | `src/AppTemplate/Markup/LocalizeExtension.cs` | `Localizer.Instance.GetString(key)` |
| `EnumLocalizationConverter` | `src/AppTemplate/Converters/EnumLocalizationConverter.cs` | `Localizer.Instance.GetString(key)` |
| `ConfirmationDialogService` | `src/AppTemplate/Services/Dialogs/ConfirmationDialogService.cs` | `Localizer.Instance.GetString(key)` |
| `DialogService` | `src/AppTemplate/Services/Dialogs/DialogService.cs` | `Localizer.Instance.GetString(key)` |
| `AppRatingService` | `src/AppTemplate/Services/Rating/AppRatingService.cs` | `Localizer.Instance.GetString(key)` |

In XAML, prefer the markup extension:

```xml
<TextBlock Text="{markup:Localize Key=WelcomeTitle}" />
```

## Consolidating onto the toolkit `Localizer` (tracking — issue #31)

The static `Localizer` is a small local helper. The shared `MZikmund.Toolkit.WinUI` package
(`0.1.21-dev.69`, pinned in [`src/Directory.Packages.props`](../src/Directory.Packages.props)) is
expected to grow an `ILocalizer` abstraction plus a static `Localizer.Current` accessor that
covers exactly this scenario. That toolkit feature has **not shipped yet** — the package
currently exposes only `IPreferences`/`Preferences`, `IDialogCoordinator`/`DialogCoordinator`,
`IXamlRootProvider`, `IAppRatingService`/`AppRatingService`, `PackageVersionExtensions`,
`ObservableCollectionExtensions`, `StableHash`, `ResourceAccessor`, `WindowExtensions`, and
`GlobalStaticResources` — so the local helper stays in place for now.

Once the toolkit ships `ILocalizer` / `Localizer.Current`, drop the local copy and consume the
toolkit version directly:

- [ ] Bump `MZikmund.Toolkit.WinUI` in `src/Directory.Packages.props` to the version that
      introduces `ILocalizer` / `Localizer.Current`.
- [ ] Delete the local [`src/AppTemplate/Services/Localization/Localizer.cs`](../src/AppTemplate/Services/Localization/Localizer.cs).
- [ ] Repoint every static consumer to the toolkit `Localizer.Current`:
      [`LocalizeExtension`](../src/AppTemplate/Markup/LocalizeExtension.cs),
      [`EnumLocalizationConverter`](../src/AppTemplate/Converters/EnumLocalizationConverter.cs),
      [`ConfirmationDialogService`](../src/AppTemplate/Services/Dialogs/ConfirmationDialogService.cs),
      [`DialogService`](../src/AppTemplate/Services/Dialogs/DialogService.cs), and
      [`AppRatingService`](../src/AppTemplate/Services/Rating/AppRatingService.cs).
- [ ] Decide whether the DI consumers (ViewModels, `WindowShell`) keep injecting
      `IStringLocalizer` or switch to the toolkit `ILocalizer`, and apply it consistently.
- [ ] Replace the `using AppTemplate.Services.Localization;` imports with the toolkit namespace
      across the touched files.

Until those preconditions are met, the local `Localizer` stays and this checklist tracks the
follow-up.
