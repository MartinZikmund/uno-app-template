# App Template

A production-shaped starting point for a cross-platform [Uno Platform](https://platform.uno/) app.
Five platform heads from a single project, plain WinUI/XAML with CommunityToolkit.Mvvm, and the
plumbing you would otherwise rebuild every time: navigation, dependency injection, theming,
localization, dialogs, versioning, and release pipelines.

Pick the parts you want, and pick up later improvements when you're ready.

## What's in the box

| | |
|---|---|
| **Six heads, one project** | Android, iOS, [tvOS](./docs/tvos.md), Windows (WinAppSDK), Desktop (Skia), and WebAssembly from `src/AppTemplate`. See [docs/building.md](./docs/building.md). |
| **MVVM with CommunityToolkit.Mvvm** | `ObservableObject`, `[ObservableProperty]` partial properties, and `[RelayCommand]`. View models live in `AppTemplate.Core` so they unit-test without a UI head. See [docs/views.md](./docs/views.md). |
| **Type-driven navigation** | `INavigationService.Navigate<TViewModel>()`, with views registered explicitly rather than by reflection. |
| **DI with the guardrails on** | Scope validation enabled, so a captive dependency fails at startup instead of in production. Per-window scopes for window-bound services. |
| **Services already wired** | Theming, preferences, dialogs and confirmations, app rating, share, launcher, display-request, and app-update checks. Keep only the ones you want. |
| **A feature picker** | A visual wizard, a terminal wizard, or plain `dotnet new`, all from one template. See [docs/template-wizard.md](./docs/template-wizard.md). |
| **An upgrade path** | Every template change leaves a note for existing apps, and a command to merge the code. See [docs/template-upgrades.md](./docs/template-upgrades.md). |
| **Localization from the start** | `{markup:Localize Key=...}` in XAML, `IStringLocalizer` in code, English and Czech resources included. |
| **Side-by-side Dev builds** | Nerdbank.GitVersioning with Dev and Prod channels that install alongside each other, with a DEV badge generated onto your icon and splash ([docs/dev-assets.md](./docs/dev-assets.md)). See [docs/versioning.md](./docs/versioning.md). Each git worktree gets its own identity too, so two can run at once — see [docs/worktree-identity.md](./docs/worktree-identity.md). |
| **CI that packages** | Every push to `main` packages all six heads and publishes nothing. XAML formatting is enforced on every PR — see [docs/xaml-styler.md](./docs/xaml-styler.md). |
| **Automated multi-store releases** | `release/vX.Y` branches ship to Google Play, TestFlight / App Store, the Microsoft Store and Cloudflare Workers, with staged rollouts and one-click approval gates. See [docs/release-pipeline.md](./docs/release-pipeline.md). |
| **Written for coding agents** | [`AGENTS.md`](./AGENTS.md) and [`.claude/rules/`](./.claude/rules/) carry the conventions an agent needs before it writes a line. |

## Create an app

```bash
dotnet tool install -g MZikmund.UnoApp.Cli

mzuno-app wizard     # visual, in your browser
mzuno-app new        # guided, in your terminal
```

Or use the template directly, which also makes it available in Visual Studio's and Rider's new project dialogs:

```bash
dotnet new install MZikmund.UnoApp.Templates
dotnet new mzuno-app -n MyApp --platforms windows --platforms desktop --rating false
```

Pick your platforms and leave out what you don't need. The name, namespaces, package ids and CI configuration are all
set for you, and the app's own `README.md` lists what's left before a first release. See
[docs/template-wizard.md](./docs/template-wizard.md).

## Keep an app up to date

Apps remember which template version and features they came from, so they can pick up later fixes:

```bash
mzuno-app upgrade            # what's new for this app
mzuno-app upgrade --patch    # three-way merge the template's changes into your code
```

See [docs/template-upgrades.md](./docs/template-upgrades.md). An app copied from this repository by hand can join in with
`mzuno-app adopt`.

## Working on the template

This repository is both a working app and the template source. Changing it means leaving existing apps a way to follow:
see [docs/template-authoring.md](./docs/template-authoring.md).

### If you're a coding agent

Read [`AGENTS.md`](./AGENTS.md) first — it points at [`.claude/rules/`](./.claude/rules/), which
carries the conventions this repo actually enforces: code style, the Core/head split, testing, git,
documentation, and how template changes reach existing apps. Adding a feature means adding a page under
[`docs/`](./docs/), never appending prose to this file.

## Quickstart

```bash
dotnet tool restore                                                   # XAML Styler, once per clone
dotnet workload restore src/AppTemplate.slnx                          # every head's workload, once per clone
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop    # fastest head
```

Other heads, per-platform prerequisites, and how to run the packaged Windows app live in
[docs/building.md](./docs/building.md).

## Docs

[`docs/`](./docs/) holds a page per topic — start at [docs/README.md](./docs/README.md).

## Versioning

This template uses Nerdbank.GitVersioning: the git height is the patch component, so `main`
produces `0.X.{height}` prerelease builds with a Dev-channel identity that installs
side-by-side with the Store version. Stable releases come from `release/v{minor}` branches.
See [docs/versioning.md](./docs/versioning.md) for the full model,
[docs/release-pipeline.md](./docs/release-pipeline.md) for what CI does with them, and
[docs/versioning-migration.md](./docs/versioning-migration.md) to apply it to an existing app.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
