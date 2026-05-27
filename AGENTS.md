# AGENTS.md

Guidance for AI coding agents working in this repository.

## Conventions

Detailed, auto-loaded conventions live in **`.claude/rules/`** — read them before writing code:
`code-style.md` (language, naming, `WarningsAsErrors`, Central Package Management),
`architecture.md` (Core/head split, MVVM, DI, navigation, localization, "how to add a page"),
`testing.md` (MSTest/MTP, run command, fakes + FluentAssertions), `git.md` (commits, branches, versioning),
and `docs.md` (feature docs go in `docs/<topic>.md` — **never** appended to `README.md`).

## Skills & external resources

Lean on installed skills and skill collections instead of reinventing — reach for them proactively:

- **Windows / WinUI** — use the [`win-dev-skills`](https://github.com/microsoft/win-dev-skills) (`winui:*`) skills *extensively*. Uno's API mirrors the WinUI API, so WinUI guidance applies almost verbatim to this app.
- **UI & design** — **`/winui-design` is the primary resource** for any layout, styling, theming, or Fluent-design work.
- **.NET** — use the [dotnet skills](https://github.com/dotnet/skills/) (`dotnet-*`, `dotnet-test:*`, `dotnet-msbuild:*`, `dotnet-upgrade:*`, …) for building, testing, performance, diagnostics, and migrations.
- **Browser automation** — when you need to load a page, screenshot, or verify web/WASM output, drive a browser with the **Playwright MCP** or the **Chrome integration** (either or both).
- **MCP docs & runtime** — use the **Microsoft Learn MCP** (`microsoft_docs_search`/`_fetch`) for authoritative .NET/Windows API docs, and the **Uno docs MCP** (`uno_platform_search`/`_fetch`) for Uno specifics — but **not** for design/UX recommendations (use `/winui-design` for those). To drive and inspect the running app at runtime, pick by target: the **Uno-app MCP** (`uno_app_select_solution` → `uno_discover_tools`/`uno_execute_tool`) for **Uno targets** (Desktop/WASM/mobile) only — it does **not** drive the WinUI/WinAppSDK head; for the **WinUI (Windows) target**, use the **`/winui-ui-testing`** skill (and `/run-winui-app` to build/launch it).

## Tech choices to avoid

This app is plain WinUI/XAML + CommunityToolkit.Mvvm by design. **Do not introduce Uno.Extensions Navigation, C# Markup, or MVUX.** More generally, prefer a WinUI / CommunityToolkit alternative over **Uno.Extensions**, **Uno Toolkit**, or **Uno Themes** whenever one exists. (The hosting/configuration/localization/serialization features already wired in `UnoFeatures` are fine to keep — this is about not adding *new* dependencies on those stacks.)

## Running & automating the WinUI (Windows) app

The app is an Uno single-project app; the WinUI (WinAppSDK) head can be built, launched **fully
packaged with package identity**, and UI-automated entirely from the command line — no Visual
Studio. Use this to see a change working on Windows, screenshot the app, or drive its UI.

Full happy path, gotchas, and the complete `winapp ui` command set live in
**`.claude/skills/run-winui-app/SKILL.md`** — read it before running the Windows head. Quick
reference (PowerShell; the WinUI TFM is the `*-windows*` entry in
`src/AppTemplate/AppTemplate.csproj`):

```powershell
# 1. Build the Windows head
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100 -c Debug

# 2. Launch packaged + detached (returns AUMID + PID; stays non-blocking so you can automate)
$out = Join-Path (Get-Location) "src\AppTemplate\bin\Debug\net10.0-windows10.0.26100"   # run from the repo root
winapp run $out --exe AppTemplate.exe --detach --json

# 3. Automate the live window (-a is the window TITLE, "App Template")
#    Workflow: inspect (find a slug) -> act (invoke/click/set-value) -> verify (get-value/wait-for).
winapp ui inspect    -a "App Template"                                    # discover element slugs
winapp ui invoke     "SettingsItem" -a "App Template"                     # press by slug or text
winapp ui screenshot -a "App Template" --output .screenshots\app.png      # -> repo-root .screenshots/ (git-ignored)

# 4. Clean up
Get-Process AppTemplate -ErrorAction SilentlyContinue | Stop-Process -Force
winapp unregister --manifest "$out\AppxManifest.xml"
```

Key gotchas: `winapp` is native Windows — pass `D:\...` paths, not `/d/...`; `--exe
AppTemplate.exe` disambiguates from the co-located `RestartAgent.exe`; `-f <winui-tfm>` is
mandatory on `dotnet run` (without it you get the WebAssembly head); don't background the app with
PowerShell `Start-Job` (it doesn't survive across tool calls) — `winapp run --detach` is the right
primitive. Requires the `winapp` CLI (`winget install Microsoft.WinAppCli`).

## Testing new features in the running app

To verify a new feature end-to-end in the live app, use the **`/run-winui-app` skill** (build →
launch packaged → inspect/act/screenshot → clean up) together with the **`uno-app` MCP**
(`uno_app_select_solution`, then `uno_discover_tools` / `uno_execute_tool`) to drive and inspect the
running app at runtime. Reach for these instead of guessing whether UI changes work — see the change
actually render before reporting it done.

## Build & test

```bash
dotnet tool restore                                                  # once after cloning (XAML Styler)

# Build a head (TFM is per-platform; WinUI TFM is the *-windows* entry in the csproj)
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100

# Run the unit tests (MSTest on Microsoft.Testing.Platform, net10.0)
dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj
```

Logic worth testing lives in **`AppTemplate.Core`** (view models, services, navigation) and belongs
under **`AppTemplate.Core.Tests`** — keep testable code there so it stays head-independent.

## Working style

- **TDD.** Write a failing test in `AppTemplate.Core.Tests` first, watch it fail, then implement until
  it passes. Prefer pushing logic into `AppTemplate.Core` so it's unit-testable without a UI head.
- **Use modern C#.** Reach for the latest C# language features (collection expressions, primary
  constructors, pattern matching, etc.) where they make the code clearer — not for their own sake.
- **Keep comments lean.** No walls of text. Comment the *why* in a line or two; let clear names and
  small methods carry the *what*.
- **XAML formatting is CI-enforced.** Run `dotnet xstyler -c Settings.XamlStyler -r -d ./src` before
  committing XAML, or the **XAML Style Check** workflow fails the PR.
- **Report Uno divergences upstream.** WinUI is the reference behavior. If during development you find
  something that's buggy or behaves differently on an Uno target (e.g. `net10.0-desktop`) than on
  WinUI, point it out and offer to file an issue at [`unoplatform/uno`](https://github.com/unoplatform/uno)
  (with a minimal repro and the affected target). Don't open it silently — confirm with the user first.

## Spec Kit

This template is set up for **[Spec Kit](https://github.com/github/spec-kit)**
([speckit.org](https://speckit.org)) — GitHub's spec-driven development toolkit.
Instead of jumping straight to code, you describe a feature, let the workflow turn
it into a reviewed spec, plan, and task list, and then implement against that plan.

The scaffolding lives in the `.specify/` directory and is committed to the repo, so
the workflow is usable without re-running the Spec Kit CLI:

```text
.specify/
├── memory/
│   └── constitution.md          # Project constitution (principles + governance) — FILLED IN
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

The constitution is the project's non-negotiable rulebook (modern C#, MVVM with
CommunityToolkit.Mvvm, testable services, MSTest test-first discipline, Fluent
Design, and localization rules). Every plan validates its `Constitution Check`
gate against it.

### The `/speckit-*` workflow

Run these commands (as slash commands in Claude Code) in order for a new feature.
Each step writes artifacts under `specs/<feature>/` and feeds the next step.

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

- Implementation MUST follow the constitution. When the UI is involved, apply the
  Fluent Design and localization rules; when adding logic, put it in testable
  `AppTemplate.Core` services with MSTest coverage.
- Run `dotnet format` before staging and build the affected project(s) before
  calling a change done.
- The `.specify/templates/*.md` files are intentionally full of `[PLACEHOLDER]`
  tokens — they are filled in per feature by the commands above. Do not "fix" the
  placeholders in the templates themselves.
