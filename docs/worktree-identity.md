# Worktree-scoped app identity

You are working on two branches at once in two git worktrees, and you want to run *both* apps at
the same time to compare them. Without help, you can't: both builds declare the same package
identity, so installing the second one replaces the first, and on desktop they share a single
app-data folder — clearing preferences in one wipes the other.

A build made from a **linked git worktree** therefore gets its own identity automatically. Nothing
to enable, nothing to remember:

```
D:\Personal\uno-app-template                              -> dev.mzikmund.apptemplate.dev
D:\Personal\uno-app-template-worktrees\worktree-identity  -> dev.mzikmund.apptemplate.dev.wtworktree1b71ff
```

Both install. Both run. Neither can see the other's settings.

## What you see

| Where | Main checkout | Worktree |
|---|---|---|
| Start menu / taskbar | `App Template Dev` | `App Template Dev (identity)` |
| Window title | `Settings` | `Settings — identity` |
| Settings → About | `0.1.92` | `0.1.92` with `Worktree: identity` beneath |
| Android / iOS home screen | `App Template` | `App Template [Iden]` / `AppTmpl [Iden]` |

## How it works

Detection is three filesystem reads in [`src/WorktreeIdentity.props`](../src/WorktreeIdentity.props),
with no `git` subprocess. That is not an optimisation — `ApplicationId` is chosen during MSBuild
*evaluation*, where `Exec` cannot run at all.

In a linked worktree, `.git` is a **file** rather than a directory:

```
$ cat .git
gitdir: D:/Personal/uno-app-template/.git/worktrees/worktree-identity
```

The build reads that file, resolves the path (git 2.48+ can write it relative), and checks that its
parent directory is `worktrees`. A submodule's `.git` is also a file, but points under `modules/`,
so it is correctly ignored.

The identifier is the git **admin-directory** name — not the folder basename and not the branch:

- Git guarantees admin names are unique. Add a second worktree whose folder is also `alpha` and git
  names its admin dir `alpha1`. Folder basenames carry no such guarantee.
- It survives a detached HEAD, where `git rev-parse --abbrev-ref HEAD` just returns `HEAD`.
- **It does not change when you switch branches.** Branch-derived identity would mint a new package
  identity mid-session and orphan the install you were just using, along with its data.

### The derived tags

| Tag | Rule | `worktree-identity` | `issue-36-spec-kit` | Used by |
|---|---|---|---|---|
| Id segment | `wt` + 8 padded alnum + 6 hex, always 16 chars | `wtworktree1b71ff` | `wtissue36s9a6639` | `ApplicationId` |
| Long | name minus a `worktree-`/`wt-` prefix, capped at 20 | `identity` | `issue-36-spec-kit` | Windows name, window title, About |
| Short | initials of 2+ segments, else first 4 chars | `Iden` | `I3SK` | Android label, iOS `CFBundleName` |
| Dev port | `5001 + (hash % 999)` | `5033` | `5566` | WebAssembly dev server |

The id keeps the **full** name while the display tags drop the `worktree-` prefix, so two worktrees
named `identity` and `worktree-identity` still get distinct identities even though both display as
`identity`.

The hash is what makes truncation safe: `worktree-identity` and `worktree-identity-v2` share their
first eight alphanumeric characters but get different segments (`…1b71ff` vs `…240028`).

## Turning it off

| You want | Do this |
|---|---|
| The canonical Dev identity, from inside a worktree | `dotnet build … -p:EnableWorktreeIdentity=false` |
| A specific name instead of the detected one | `dotnet build … -p:AppWorktreeName=whatever` |
| A release build | Nothing — `AppChannel=Prod` is never suffixed |

`-p:AppWorktreeName` sets the name but **cannot** switch the feature on where it is off: the
channel, CI and kill-switch guards all sit on the *application* step, not on detection.

## Guarantees

These are asserted by [`scripts/verify-worktree-identity.ps1`](../scripts/verify-worktree-identity.ps1),
which is worth running after touching either MSBuild file:

```powershell
pwsh scripts/verify-worktree-identity.ps1
```

| # | Guarantee |
|---|---|
| I1 | `AppChannel=Prod` is never suffixed, whatever is passed on the command line or exported |
| I2 | The main checkout produces exactly the identity it did before this feature existed |
| I3 | CI does too — `actions/checkout` makes a real clone, plus explicit `CI` / `ContinuousIntegrationBuild` gates |
| I4 | The tracked `Package.appxmanifest` is never written to |
| I5 | `ApplicationId` stays ≤ 50 chars, letter-first, `[a-z0-9.]` only |
| I6 | `ApplicationTitle` stays ≤ 40 chars |

I5 and I6 are not decoration. MSIX caps `Identity/@Name` at 50 characters and
`uap:DefaultTile/@ShortName` at 40 (`ST_ShortDisplayName`); `App Template Dev (` plus `)` already
costs 19 of those 40, which is exactly why the long tag is capped at 20. iOS caps `CFBundleName`
under 16, which is why the base abbreviates to `AppTmpl` there.

## Per platform

| Head | Isolated by | Notes |
|---|---|---|
| **WinUI / MSIX** | `Identity/@Name` → package family name, AUMID, Start entry, `%LOCALAPPDATA%\Packages\<PFN>\` | Fully isolated. The primary target |
| **Desktop (Skia)** | app-data folder derives from `ApplicationId` | Verify with `Get-ChildItem "$env:LOCALAPPDATA\Martin Zikmund"` |
| **Android** | `package=` and the derived content-provider authority | Both APKs install side by side |
| **iOS** | `CFBundleIdentifier` | **Simulator only.** A per-worktree bundle id matches no provisioning profile, so device deploys need a wildcard App ID with automatic signing |
| **WebAssembly** | nothing — isolation is the browser origin | See below |

### Localised names are preserved

The Android launcher label and the iOS home-screen name are localised (`values/` + `values-cs/`,
`en.lproj/` + `cs.lproj/`). Replacing them with a generated constant would have silently dropped
Czech. Instead the build regenerates **each locale's** resource file into `obj/` with the tag
appended, and repoints the resource item at the copy. `Main.Android.cs` still reads
`Label = "@string/ApplicationName"`, and translators keep editing the tracked files.

### WebAssembly

WASM has no install identity; two worktrees would fight over `http://localhost:5000` and, because
Uno's storage keys carry no app id, share one origin's `localStorage`.

Building the WASM head from a worktree prints the port to use:

```
Worktree 'worktree-identity': serve this head on its own origin with --urls http://localhost:5033
```

Pass it through:

```bash
dotnet run -f net10.0-browserwasm --urls http://localhost:5033
```

The launch profiles in `Properties/launchSettings.json`, `.vscode/launch.json` and
`.run/AppTemplate.run.xml` are deliberately **not** rewritten per worktree — they are tracked files,
and editing them on every feature branch is exactly the recurring-merge-conflict pattern that
[`.claude/rules/docs.md`](../.claude/rules/docs.md) exists to prevent.

## Running two at once

```powershell
# From each worktree, in its own shell:
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100 -c Debug
$out = Join-Path (Get-Location) "src\AppTemplate\bin\Debug\net10.0-windows10.0.26100"
$app = winapp run $out --exe AppTemplate.exe --detach --json | ConvertFrom-Json
```

Then target each app by the **PID** it returned, not by window title:

```powershell
winapp ui screenshot -a $app.ProcessId --output .screenshots\this-one.png
```

`$out` is built from `Get-Location`, so run these from the worktree you actually mean — otherwise
you register and launch the other one's build.

Cleaning up is per-worktree, because `unregister` reads the generated manifest's identity:

```powershell
Stop-Process -Id $app.ProcessId -Force          # NOT Get-Process AppTemplate: that kills every worktree
winapp unregister --manifest "$out\AppxManifest.xml"
```

Strays accumulate over time. Find them with:

```powershell
Get-AppxPackage dev.mzikmund.apptemplate.dev.wt* | Select-Object Name, PackageFamilyName
```

## Limitations

- **`git worktree move` changes the identity.** The admin directory is renamed, so the build becomes
  a different package and the old install plus its data is orphaned. Uninstall before moving.
- **`Get-Process AppTemplate` matches every worktree.** `AssemblyName` is deliberately unchanged —
  the launch configs, the WASM linker config and `--exe AppTemplate.exe` all depend on it.
- **Icons are identical across worktrees.** `UnoIconBackgroundFile` must stay constant (the `UnoIcon`
  item's `Include` *is* the background, and varying it renames the Android resource and breaks
  `@mipmap/icon`), and the one foreground slot already belongs to the Dev channel.
- **A local `-p:AppChannel=Prod` build from a worktree overwrites the real Prod app.** That is the
  safe direction — no worktree-suffixed id can ever ship — but it is worth knowing.
- **The Desktop head's manifest version stays `1.0.0.1`**, because `SetNbgvVersionForUnoWindows` is
  gated on `TargetPlatformIdentifier == 'windows'`. Pre-existing and unrelated, but the About card's
  version line is already wrong there, and the new worktree line sits right next to it.
- **The iOS overlay is unverified on Windows.** It follows the Android overlay's shape, which is
  verified; confirm it on a Mac before relying on it.

## If you add features later

Two rules that are cheap to honour now and expensive to retrofit:

1. **A SQLite database must be rooted at `ApplicationData.Current.LocalFolder`.** `sqlite-net-e` is
   referenced but nothing constructs a connection yet. Rooted there, it is isolated per worktree for
   free; rooted anywhere else, every worktree shares one file.
2. **Machine-global registrations are not covered by `ApplicationId`.** `Package.appxmanifest` has no
   `<Extensions>` element today. The moment one appears — a toast COM activator CLSID, an
   `apptemplate://` protocol handler, a file-type association — it registers per *machine*, and two
   worktrees will fight over it. That needs separate handling.

## See also

- [versioning.md](./versioning.md) — the Dev/Prod `AppChannel` model this layers on top of.
- [building.md](./building.md) — target frameworks and per-platform build commands.
- [`.claude/skills/run-winui-app/SKILL.md`](../.claude/skills/run-winui-app/SKILL.md) — the full
  build → launch → automate → clean up loop for the Windows head.
