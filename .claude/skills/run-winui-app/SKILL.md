---
name: run-winui-app
description: >-
  Build, launch, and UI-automate the WinUI (Windows / WinAppSDK) head of this Uno app from the
  command line — no Visual Studio. Use this whenever the user wants to run, start, launch, or
  screenshot the Windows/WinUI/WinAppSDK target, see a change working in the real Windows app,
  verify a fix on Windows, or drive the app's UI (click buttons, inspect the visual tree, set
  text, wait for elements). Trigger even if the user just says "run the app on Windows", "show me
  the Windows app", "take a screenshot of the app", or "click Settings in the app" without naming
  WinUI explicitly. Launches a fully packaged app (with package identity) via `winapp run` and
  automates it via `winapp ui`.
---

# Run & automate the WinUI app

This Uno single-project app has a WinUI (WinAppSDK) head. This skill builds it, launches it
**fully packaged with package identity** straight from the CLI (no Visual Studio, no manual
`Add-AppxPackage`), and drives its UI with the Windows App Development CLI (`winapp`).

Use it when you need to see something working in the real Windows app — a layout change, a new
page, a bug fix — or when the user asks to click around, screenshot, or inspect the running app.

## Why two tools

- **`dotnet build` / `dotnet run`** compiles the head and produces a loose-layout package folder.
- **`winapp`** (Windows App Development CLI, v0.3+) registers that loose layout with a development
  package identity, launches it, and exposes UI Automation (`winapp ui`) so you can inspect and
  interact with the live window. It works against any running Windows app (WinUI 3, WPF, WinForms,
  Win32, Electron), so the same `ui` commands work here.

## Prerequisites (usually already satisfied)

- .NET 10 SDK (`dotnet --version` → 10.x). The repo's `global.json` pins the Uno SDK.
- `winapp` CLI: `winapp --version` should print `0.3.x` or newer. If missing:
  `winget install Microsoft.WinAppCli` (or `npm install -g @microsoft/winappcli`).
- No extra NuGet package is needed — `Uno.Sdk` already emits a packaged loose layout with an
  `AppxManifest.xml` for the Windows head.

## The happy path

Run these from PowerShell. `winapp` is a Windows executable, so always pass **Windows-style
paths** (`D:\...`), never bash `/d/...` paths.

### 1. Find the WinUI target framework

It is the `*-windows*` entry in `<TargetFrameworks>` of `src/AppTemplate/AppTemplate.csproj`
(currently `net10.0-windows10.0.26100`, but read it from the csproj in case it has bumped). Call
it `$TFM` below. The output folder is `src/AppTemplate/bin/Debug/$TFM/` and the launchable exe is
`AppTemplate.exe` (it matches the project/assembly name — note `RestartAgent.exe` also sits in
that folder, which is why we disambiguate with `--exe` below).

### 2. Build the head

```powershell
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100 -c Debug
```

`AppChannel` defaults to `Dev` (see `src/Directory.Build.props`), so the package identity
(`dev.mzikmund.apptemplate.dev`) and the `DEV` corner badge are set automatically — no extra
flags. A first build takes ~1 min; rebuilds are fast.

### 3. Launch — pick the path that fits

**Path A — automation (recommended for an agent).** `winapp run` with `--detach` registers the
package, launches it, and **returns immediately** with the AUMID and PID, so you stay in control
to automate or screenshot it:

```powershell
$out = "D:\Personal\uno-app-template\src\AppTemplate\bin\Debug\net10.0-windows10.0.26100"
winapp run $out --exe AppTemplate.exe --detach --json
# -> { "AUMID": "dev.mzikmund.apptemplate.dev_...!App", "ProcessId": 470772 }
```

Useful extra flags: `--unregister-on-exit` (auto-clean when the app closes), `--clean` (wipe the
app's LocalState/settings before deploying), `--debug-output` (stream OutputDebugString + first-
chance exceptions; can't be combined with `--detach`).

**Path B — simple foreground run (for a human watching).** This **blocks the terminal** until the
app closes, so don't use it when you need to run more commands afterward:

```powershell
dotnet run --project src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100 -c Debug
```

The `-f net10.0-windows10.0.26100` is **required**: `launchSettings.json` lists the WebAssembly
profile first, so a bare `dotnet run` would launch WASM. Passing the Windows TFM makes `dotnet
run` auto-select the compatible `MsixPackage` profile and launch the packaged WinUI app.

> Do not use PowerShell `Start-Job` to background the app: background jobs do **not** survive
> across separate tool calls. `winapp run --detach` launches a real detached OS process that
> persists — that's the correct primitive for an agent.

### 4. Automate the running app with `winapp ui`

Target the app with `-a "App Template"`. That value is the **window title**, which is
`App Template` for both channels (the manifest DisplayName `App Template Dev` is *not* the window
title). If unsure, discover it first:

```powershell
winapp ui list-windows                 # all windows; find yours by process "AppTemplate"
winapp ui list-windows -a "App Template"
winapp ui status      -a "App Template"   # confirm connection (process, PID, HWND)
```

Then inspect and interact (elements are addressed by a **semantic slug** shown in `inspect`, or by
visible text):

```powershell
winapp ui inspect    -a "App Template"                     # element tree w/ slugs, types, bounds
winapp ui screenshot -a "App Template" --output shot.png   # PNG of the live window
winapp ui search     "Settings" -a "App Template"          # find elements by text
winapp ui invoke     "Settings" -a "App Template"          # activate by slug OR visible text
winapp ui click      "<slug>"   -a "App Template"          # mouse click (for non-invokable items)
winapp ui set-value  "<slug>" "Hello" -a "App Template"    # type into a TextBox/ComboBox/Slider
winapp ui get-value  "<slug>" -a "App Template"
winapp ui wait-for   "<slug>" -a "App Template" -timeout 10000
```

To confirm a screenshot visually, read the PNG file back with the Read tool.

### 5. Clean up

```powershell
Get-Process AppTemplate -ErrorAction SilentlyContinue | Stop-Process -Force
winapp unregister --manifest "$out\AppxManifest.xml"
```

`winapp unregister` only removes development-mode registrations (it's a no-op otherwise, which is
safe). Skip the manual stop if you launched with `--unregister-on-exit` and the app has closed.

## Gotchas worth remembering

- **Paths:** `winapp` is native Windows — give it `D:\...` paths. The Bash tool's working
  directory also persists between calls, so prefer absolute paths over `cd`.
- **`--exe AppTemplate.exe`:** the output folder contains both `AppTemplate.exe` and
  `RestartAgent.exe`; `winapp run` needs the disambiguation.
- **`-a` matches the window title** (`App Template`), not the manifest DisplayName.
- **`-f` on `dotnet run` is mandatory** — otherwise you get the WebAssembly head.
- **Don't background with `Start-Job`** — use `winapp run --detach`.
- Run `winapp <command> --help` (e.g. `winapp run --help`, `winapp ui --help`) for the full,
  current flag list; the CLI also supports `--cli-schema` for machine-readable command structure.

## Reference

- Background notes & verification log: `docs/winui-run-notes.md`.
- dotnet run for packaged apps:
  https://devblogs.microsoft.com/ifdef-windows/introducing-dotnet-new-templates-for-winui/
- Windows App Development CLI v0.3 (`run` + `ui`):
  https://devblogs.microsoft.com/ifdef-windows/windows-app-development-cli-v0-3-new-run-and-ui-commands-plus-dotnet-run-support-for-packaged-apps/
