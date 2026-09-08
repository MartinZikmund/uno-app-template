# Building & running

## Restore dotnet tools

The repository ships a local tool manifest (`.config/dotnet-tools.json`). Restore the tools once after cloning:

```bash
dotnet tool restore
```

This installs [XAML Styler](./xaml-styler.md), which CI enforces on every pull request.

## Target frameworks

`src/AppTemplate` is an Uno single project that builds five heads. The target framework moniker
selects which one:

| TFM | Platform | Prerequisites |
|---|---|---|
| `net10.0-desktop` | Cross-platform desktop (Windows, macOS, Linux), Skia-rendered | None beyond the .NET SDK — the fastest head to build |
| `net10.0-browserwasm` | WebAssembly | None beyond the .NET SDK |
| `net10.0-windows10.0.26100` | Windows, WinAppSDK | Windows + the Windows SDK |
| `net10.0-android` | Android | Android workload |
| `net10.0-ios` | iOS | iOS workload, and a Mac to build against |

Prefer a single-target build. The default multi-target build pulls the Android SDK, which fails on
network-restricted machines and is rarely what you want while iterating.

## Build and run

```bash
# Cross-platform desktop — the quickest feedback loop
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop
dotnet run   --project src/AppTemplate/AppTemplate.csproj -f net10.0-desktop

# Windows (WinAppSDK)
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100
```

`-f` is mandatory on `dotnet run`. Without it you get the WebAssembly head.

## Running the packaged Windows app

The WinUI head can be built, launched with full package identity, and UI-automated entirely from
the command line — no Visual Studio. It needs the [`winapp`](https://www.nuget.org/packages/Microsoft.WinAppCli)
CLI (`winget install Microsoft.WinAppCli`):

```powershell
$out = Join-Path (Get-Location) "src\AppTemplate\bin\Debug\net10.0-windows10.0.26100"
$app = winapp run $out --exe AppTemplate.exe --detach --json | ConvertFrom-Json   # non-blocking
$AppPid = $app.ProcessId

winapp ui inspect    -a $AppPid                            # discover element slugs
winapp ui screenshot -a $AppPid --output .screenshots\app.png

Stop-Process -Id $AppPid -Force -ErrorAction SilentlyContinue
winapp unregister --manifest "$out\AppxManifest.xml"
```

`--exe AppTemplate.exe` disambiguates from the co-located `RestartAgent.exe`.

Target the **PID**, not the window title, and stop the PID rather than the process name. Both
matter once you use worktrees: a worktree build carries its own package identity and window title,
while `AssemblyName` stays `AppTemplate` everywhere — so `Get-Process AppTemplate | Stop-Process`
kills every worktree's app. See [worktree-identity.md](./worktree-identity.md).

The full command set and its gotchas live in
[`.claude/skills/run-winui-app/SKILL.md`](../.claude/skills/run-winui-app/SKILL.md) — that file is
canonical; this is a summary.

## Tests

Unit tests cover `AppTemplate.Core`, which is why logic belongs there rather than in the head:

```bash
dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj
```

The runner is MSTest on Microsoft.Testing.Platform, not VSTest — VSTest-only flags such as
`--logger` and `--nologo` will error out. Conventions are in
[`.claude/rules/testing.md`](../.claude/rules/testing.md).

## When a build hangs

A `dotnet build` or `dotnet test` that stops producing output while MSBuild processes linger is
usually the local node-reuse deadlock. Re-run with `MSBUILDDISABLENODEREUSE=1` set. Don't pass
`-nodeReuse:false` to `dotnet test` — it silently runs zero tests.
