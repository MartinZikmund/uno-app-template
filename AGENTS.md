# AGENTS.md

Guidance for AI coding agents working in this repository.

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
$out = "D:\Personal\uno-app-template\src\AppTemplate\bin\Debug\net10.0-windows10.0.26100"
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

Logic worth testing lives in **`AppTemplate.Core`** (view models, services, navigation) and is
covered by **`AppTemplate.Core.Tests`** — keep testable code there so it stays head-independent.

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
