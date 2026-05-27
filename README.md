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

## Swappable cleanup with SerialDisposable

When a ViewModel tracks the *current* item — and that item owns a resource that must be released the moment the selection moves on — you need to dispose the previous resource before acquiring a new one. `Uno.Disposables.SerialDisposable` makes that hand-off concise and exception-safe.

A typical case: only the selected item should keep the screen awake. `IDisplayRequestManager.RequestActive()` returns an `IDisposable` that holds the request until it is disposed. As the selection changes, the previous request must be released so just one stays active.

### How it works

`SerialDisposable` holds a single inner `IDisposable`. Assigning a new value to its `.Disposable` property automatically disposes whatever it held before. Assigning `null` disposes the current value and holds nothing. This removes the need for a manual null check and a separate `Dispose()` call before every reassignment.

### Example

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Uno.Disposables;

namespace AppTemplate.Core.ViewModels;

public partial class ItemListViewModel : ViewModelBase, IDisposable
{
    private readonly IDisplayRequestManager _displayRequestManager;
    private readonly SerialDisposable _displayRequestDisposable = new();

    public ItemListViewModel(IDisplayRequestManager displayRequestManager)
    {
        _displayRequestManager = displayRequestManager;
    }

    [ObservableProperty]
    public partial ItemModel? SelectedItem { get; set; }

    partial void OnSelectedItemChanged(ItemModel? value)
    {
        // Assigning here releases the previous item's display request before
        // acquiring one for the new selection. Assigning null releases it entirely.
        _displayRequestDisposable.Disposable = value is { KeepScreenOn: true }
            ? _displayRequestManager.RequestActive()
            : null;
    }

    public void Dispose()
    {
        // Releases the currently held display request (if any).
        _displayRequestDisposable.Dispose();
    }
}
```

The same idiom wraps any cleanup that should run on the next swap. Pass a callback to `Disposable.Create(...)` — for example, to unhook an event handler that was attached for the current selection:

```csharp
_displayRequestDisposable.Disposable = Disposable.Create(() =>
{
    value.SomethingChanged -= OnSomethingChanged;
});
```

### When to prefer `SerialDisposable` over manual dispose-then-reassign

| Concern | Manual pattern | `SerialDisposable` |
|---|---|---|
| Null-safety | Requires an explicit null check before calling `.Dispose()` | Handles `null` automatically |
| Exception-safety | A throw between `Dispose()` and reassignment leaks the old resource | Assignment is atomic — the old value is always disposed |
| Readability | Two statements for every "swap" | One statement |

### Teardown

Dispose the `SerialDisposable` field when the ViewModel is torn down (implement `IDisposable` and call `_displayRequestDisposable.Dispose()`) so the final active resource is released once the ViewModel is gone.

If you want to release the resource earlier — while keeping the ViewModel alive — clear it from a `ViewModelBase` lifecycle hook such as `ViewUnloaded()` or `OnNavigatedFrom()` by assigning `_displayRequestDisposable.Disposable = null;`.

## Versioning

This template uses Nerdbank.GitVersioning. `main` produces `0.X.0-dev.{height}` prerelease builds with a Dev-channel identity that installs side-by-side with the Store version. Stable releases come from `release/v{minor}` branches. See [docs/versioning.md](./docs/versioning.md) for the full model and [docs/versioning-migration.md](./docs/versioning-migration.md) to apply it to an existing app.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
