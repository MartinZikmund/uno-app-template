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
