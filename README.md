# App Template

A production-shaped starting point for a cross-platform [Uno Platform](https://platform.uno/) app.
Five platform heads from a single project, plain WinUI/XAML with CommunityToolkit.Mvvm, and the
plumbing you would otherwise rebuild every time: navigation, dependency injection, theming,
localization, dialogs, versioning, and release pipelines.

Copy it, rename it, delete what you don't need.

## What's in the box

| | |
|---|---|
| **Five heads, one project** | Android, iOS, Windows (WinAppSDK), Desktop (Skia), and WebAssembly from `src/AppTemplate`. See [docs/building.md](./docs/building.md). |
| **MVVM with CommunityToolkit.Mvvm** | `ObservableObject`, `[ObservableProperty]` partial properties, and `[RelayCommand]`. View models live in `AppTemplate.Core` so they unit-test without a UI head. See [docs/views.md](./docs/views.md). |
| **Type-driven navigation** | `INavigationService.Navigate<TViewModel>()`, with views registered explicitly rather than by reflection. |
| **DI with the guardrails on** | Scope validation enabled, so a captive dependency fails at startup instead of in production. Per-window scopes for window-bound services. |
| **Services already wired** | Theming, preferences, dialogs and confirmations, app rating, share, launcher, display-request, and app-update checks. |
| **Localization from the start** | `{markup:Localize Key=...}` in XAML, `IStringLocalizer` in code, English and Czech resources included. |
| **Side-by-side Dev builds** | Nerdbank.GitVersioning with Dev and Prod channels that install alongside each other, distinct icons included. See [docs/versioning.md](./docs/versioning.md). |
| **CI that packages** | Build and smoke-test workflows plus Windows, Android, iOS packaging and WebAssembly deployment. XAML formatting is enforced on every PR — see [docs/xaml-styler.md](./docs/xaml-styler.md). |
| **Written for coding agents** | [`AGENTS.md`](./AGENTS.md) and [`.claude/rules/`](./.claude/rules/) carry the conventions an agent needs before it writes a line. |

## Using this template

There is no rename script — the steps below are the whole job, and doing them by hand once is
clearer than debugging a script that half-worked.

1. **Start your repo.** Use this repository as a GitHub template, or clone it and point `origin`
   at your own remote.

2. **Rename `AppTemplate` to your app.** It appears in roughly 59 C# namespace declarations plus:

   ```text
   src/AppTemplate/                          folder + AppTemplate.csproj
   src/AppTemplate.Core/                     folder + AppTemplate.Core.csproj
   tests/AppTemplate.Core.Tests/             folder + .csproj
   src/AppTemplate.slnx
   src/.run/AppTemplate.run.xml
   src/.vscode/launch.json, tasks.json
   src/AppTemplate/Properties/launchSettings.json
   src/AppTemplate/Platforms/WebAssembly/LinkerConfig.xml
   ```

   A find-and-replace of `AppTemplate` → `YourApp` across the repo, then renaming the folders and
   project files, covers all of it.

3. **Claim your identity.** In `src/AppTemplate/AppTemplate.csproj`, set `ApplicationPublisher`,
   and set `ApplicationTitle` and `ApplicationId` for **both** the `Prod` and `Dev` channel
   property groups — they must differ, that's what lets Dev install side by side. Then update the
   display names in `src/AppTemplate/Platforms/Android/Resources/values*/Strings.xml`.

4. **Replace the artwork.** Drop your own SVGs into `src/AppTemplate/Assets/Icons` and
   `src/AppTemplate/Assets/Splash`. Keep `icon_transparent.svg` and `icon.svg` as the background
   filenames unless you also update the `UnoIcon*` properties — the generated Android
   `@mipmap/icon` resource name is derived from them.

5. **Reset the version.** `version.json` starts at `0.1`. Set it to whatever your first release
   should be; git height supplies the rest.

6. **Translate or trim.** Keep both `src/AppTemplate/Strings/en` and `.../cs`, or delete the `cs`
   folder and its Android `values-cs` counterpart if you only ship one language.

7. **Delete what you don't need.** Sample views, the Czech resources, the rating service — none of
   it is load-bearing. Removing a service means deleting its files and its registration in
   `App.RegisterServices`.

### If you're a coding agent

Read [`AGENTS.md`](./AGENTS.md) first — it points at [`.claude/rules/`](./.claude/rules/), which
carries the conventions this repo actually enforces: code style, the Core/head split, testing, git,
and documentation. Adding a feature means adding a page under [`docs/`](./docs/), never appending
prose to this file.

## Quickstart

```bash
dotnet tool restore                                                   # XAML Styler, once per clone
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop    # fastest head, no workloads
```

Other heads, per-platform prerequisites, and how to run the packaged Windows app live in
[docs/building.md](./docs/building.md).

## Docs

[`docs/`](./docs/) holds a page per topic — start at [docs/README.md](./docs/README.md).

A cross-platform [Uno Platform](https://platform.uno/) (WinUI) application targeting .NET 10.

## Localization

Localized strings come from Uno's `.UseLocalization()` host extension (configured in
[`src/AppTemplate/App.xaml.cs`](src/AppTemplate/App.xaml.cs)), which registers a
`Microsoft.Extensions.Localization.IStringLocalizer` backed by the `.resw` files under
[`src/AppTemplate/Strings/`](src/AppTemplate/Strings/).

There are two ways the app resolves that `IStringLocalizer`:

- **Dependency injection**, wherever constructor injection is available (ViewModels,
  `WindowShell`). These get an `IStringLocalizer` directly from the container.
- **The static [`Localizer`](src/AppTemplate/Services/Localization/Localizer.cs) accessor**,
  for the contexts that *cannot* use constructor injection — XAML markup extensions, value
  converters, and services that build dialogs ad hoc. `Localizer.Instance` lazily resolves the
  *same* `IStringLocalizer` from the container via the `IoC` service locator
  ([`src/AppTemplate.Core/Infrastructure/IoC.cs`](src/AppTemplate.Core/Infrastructure/IoC.cs))
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

### Consolidating onto the toolkit `Localizer` (tracking — issue #31)

The static `Localizer` is a small local helper. The shared `MZikmund.Toolkit.WinUI` package
(`0.1.21-dev.69`, pinned in [`src/Directory.Packages.props`](src/Directory.Packages.props)) is
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
- [ ] Delete the local [`src/AppTemplate/Services/Localization/Localizer.cs`](src/AppTemplate/Services/Localization/Localizer.cs).
- [ ] Repoint every static consumer to the toolkit `Localizer.Current`:
      [`LocalizeExtension`](src/AppTemplate/Markup/LocalizeExtension.cs),
      [`EnumLocalizationConverter`](src/AppTemplate/Converters/EnumLocalizationConverter.cs),
      [`ConfirmationDialogService`](src/AppTemplate/Services/Dialogs/ConfirmationDialogService.cs),
      [`DialogService`](src/AppTemplate/Services/Dialogs/DialogService.cs), and
      [`AppRatingService`](src/AppTemplate/Services/Rating/AppRatingService.cs).
- [ ] Decide whether the DI consumers (ViewModels, `WindowShell`) keep injecting
      `IStringLocalizer` or switch to the toolkit `ILocalizer`, and apply it consistently.
- [ ] Replace the `using AppTemplate.Services.Localization;` imports with the toolkit namespace
      across the touched files.

Until those preconditions are met, the local `Localizer` stays and this checklist tracks the
follow-up.

## Versioning

This template uses Nerdbank.GitVersioning. `main` produces `0.X.0-dev.{height}` prerelease builds with a Dev-channel identity that installs side-by-side with the Store version. Stable releases come from `release/v{minor}` branches. See [docs/versioning.md](./docs/versioning.md) for the full model and [docs/versioning-migration.md](./docs/versioning-migration.md) to apply it to an existing app.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
