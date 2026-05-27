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

A cross-platform [Uno Platform](https://platform.uno/) (WinUI) C# application
targeting .NET 10.

## Spec-driven development with Spec Kit

This repository uses [Spec Kit](https://github.com/github/spec-kit)
([speckit.org](https://speckit.org)), GitHub's spec-driven development toolkit.
Instead of jumping straight to code, you describe a feature, let the workflow
turn it into a reviewed spec, plan, and task list, and then implement against
that plan.

The scaffolding lives in the `.specify/` directory and is committed to the repo,
so the workflow is usable without re-running the Spec Kit CLI:

```text
.specify/
├── memory/
│   └── constitution.md          # Project constitution (principles + governance)
├── templates/
│   ├── constitution-template.md # Source template for the constitution
│   ├── spec-template.md         # Feature specification template
│   ├── plan-template.md         # Implementation plan template
│   ├── tasks-template.md        # Task list template
│   └── checklist-template.md    # Custom checklist template
├── scripts/powershell/          # Prerequisite / setup helper scripts
├── extensions/git/              # Optional Git automation extension
└── integrations/ + workflows/   # Spec Kit ↔ Claude wiring
```

The [constitution](.specify/memory/constitution.md) is the project's
non-negotiable rulebook (modern C#, MVVM with CommunityToolkit.Mvvm, testable
services, MSTest test-first discipline, Fluent Design, and localization rules).
Every plan validates its `Constitution Check` gate against it.

### The `/speckit-*` workflow

Run these commands (as slash commands in Claude Code) in order for a new
feature. Each step writes artifacts under `specs/<feature>/` and feeds the next
step.

| Command | Purpose |
| --- | --- |
| `/speckit-constitution` | Create or amend the project constitution and keep dependent templates in sync. Run once up front, then only when principles change. |
| `/speckit-specify` | Turn a natural-language feature description into `spec.md` (user stories, requirements, success criteria). |
| `/speckit-clarify` | Ask up to 5 targeted questions to resolve underspecified areas, encoding answers back into the spec. |
| `/speckit-plan` | Produce the implementation plan and design artifacts (`plan.md`, `research.md`, `data-model.md`, etc.); validates the Constitution Check gate. |
| `/speckit-tasks` | Generate a dependency-ordered `tasks.md` grouped by user story. |
| `/speckit-analyze` | Non-destructive consistency/quality check across `spec.md`, `plan.md`, and `tasks.md`. |
| `/speckit-checklist` | Generate a custom review checklist for the feature. |
| `/speckit-implement` | Execute the tasks in `tasks.md`. |
| `/speckit-taskstoissues` | Convert tasks into GitHub issues (optional). |

Optional Git automation (under `.specify/extensions/git/`) provides
`/speckit-git-feature`, `/speckit-git-commit`, `/speckit-git-initialize`,
`/speckit-git-remote`, and `/speckit-git-validate` for branch creation, commits,
and branch-name validation.

A typical flow:

```text
/speckit-specify  Add an onboarding flow that ...
/speckit-clarify
/speckit-plan
/speckit-tasks
/speckit-implement
```

### Conventions to keep in mind

- Implementation MUST follow the [constitution](.specify/memory/constitution.md).
  When the UI is involved, apply the Fluent Design and localization rules; when
  adding logic, put it in testable `AppTemplate.Core` services with MSTest
  coverage.
- Run `dotnet format` before staging and build the affected project(s) before
  calling a change done.
- The `.specify/templates/*.md` files are intentionally full of `[PLACEHOLDER]`
  tokens — they are filled in per feature by the commands above. Do not "fix"
  the placeholders in the templates themselves.

## Versioning

This template uses Nerdbank.GitVersioning. `main` produces `0.X.0-dev.{height}` prerelease builds with a Dev-channel identity that installs side-by-side with the Store version. Stable releases come from `release/v{minor}` branches. See [docs/versioning.md](./docs/versioning.md) for the full model and [docs/versioning-migration.md](./docs/versioning-migration.md) to apply it to an existing app.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
