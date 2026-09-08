# Worktree-scoped app identity — design

**Date:** 2026-09-06
**Status:** Proposed
**Builds on:** [`2026-05-28-versioning-redesign-design.md`](2026-05-28-versioning-redesign-design.md) — the `AppChannel` model this layers onto.
**Applies to:** `uno-app-template` and (via `docs/versioning-migration.md`) downstream apps.

---

## 1. Model

> **A build made from a linked git worktree gets its own package identity, so two worktrees can be installed and run side by side on the same machine or device. The suffix is derived from the worktree's git admin-directory name, is gated to `AppChannel=Dev`, and is a no-op in the main checkout and in CI.**

Today two worktrees produce byte-identical identity. On Windows the second `winapp run` silently re-registers over the first; on Desktop both processes share
`%LOCALAPPDATA%\<publisher>\<ApplicationId>\`, so preferences and any future SQLite database are one shared store. This is the failure the design removes.

Worktree identity is an **orthogonal suffix**, never a third `AppChannel` value. `ApplicationTitle`/`ApplicationId` are set *only* inside the two mutually-exclusive
`AppChannel` groups in `src/AppTemplate/AppTemplate.csproj` (lines 20–28) with no unconditional fallback, so a third channel value would leave both unset.

### 1.1 Goals

- Two worktrees install and run simultaneously on Windows, Desktop, Android and iOS without overwriting each other or sharing app data.
- The running app states which worktree it came from, next to the version number in About.
- Zero effect on the main checkout, on CI, and on anything `AppChannel=Prod`. Identity there stays byte-identical to today.
- No new package dependency, no new tracked per-worktree file, no `git` subprocess in the build.

### 1.2 Non-goals

- Per-worktree app icons. `UnoIconBackgroundFile` must stay constant (the `UnoIcon` item `Include` *is* the background, so varying it renames the Android resource and
  yields `APT2260: resource mipmap/icon not found` — see `docs/versioning-migration.md` step 5). The single `UnoIconForegroundFile` slot is already owned by the Dev channel.
- Renaming `AssemblyName`. `AppTemplate` is depended on by `--exe AppTemplate.exe`, `bin/Debug/net10.0-desktop/AppTemplate.dll` in `src/.vscode/launch.json`, and
  `<assembly fullname="AppTemplate" />` in `Platforms/WebAssembly/LinkerConfig.xml`. A consequence: `Get-Process AppTemplate | Stop-Process` remains machine-wide.
- Machine-global registrations (toast COM CLSIDs, protocol handlers, file associations). `src/AppTemplate/Package.appxmanifest` has **no `<Extensions>` element at all**, so
  there is nothing to isolate today — but `ApplicationId` isolation would *not* cover them, which is recorded as a forward-looking rule in §9.

---

## 2. Detection

Detection must run at **evaluation time**, because `ApplicationId`/`ApplicationTitle` are chosen in evaluation-time `PropertyGroup`s. `Exec` cannot participate there, and
a measured `git rev-parse` costs ~78 ms per call across 11 project instances. So detection is three `System.IO` property functions and no subprocess.

| Checkout shape | `.git` | Result |
|---|---|---|
| Main checkout | directory | not a worktree — no-op |
| Linked worktree | file: `gitdir: <common>/.git/worktrees/<name>` | **detected**, name = `<name>` |
| Submodule | file: `gitdir: <common>/.git/modules/<name>` | rejected by the `worktrees` parent check |
| CI (`actions/checkout@v4`) | directory (real clone) | no-op by construction |
| Not a repo | absent | no-op |

Two details that must not be simplified away:

- **Relative gitdir paths.** Git 2.48+ can write relative paths (`git worktree add --relative-paths`, `worktree.useRelativePaths`). The
  `GetFullPath(Combine(repoRoot, raw))` normalisation handles both absolute and relative forms.
- **`_RepoRoot` empty case.** If `GetDirectoryNameOfFileAbove` finds no `version.json`, `Path.Combine('', '.git')` resolves against the MSBuild process CWD and detection
  becomes nondeterministic. Guard on a non-empty `_RepoRoot`.

### 2.1 Why the git admin-directory name

Not the working-directory basename, and **not the branch**:

- Git guarantees admin-dir names are unique — a second worktree whose path basename is also `alpha` gets admin dir `alpha1`. Path basenames carry no such guarantee.
- It survives `--detach`, where `git rev-parse --abbrev-ref HEAD` returns the literal string `HEAD`.
- It does not churn when you switch branches inside a worktree. Branch-derived identity would mint a new package identity mid-session and orphan the previous
  install *and its app data*.

`git worktree move` renames the admin directory and therefore does orphan the previous install. That is an accepted, documented caveat (§9).

---

## 3. Derived values

One raw name in, four values out. All are computed at evaluation time except the short tag's consumers (§6.3), which run in targets.

### 3.1 Sanitisation and escaping

The raw name comes from a filesystem path and reaches three escaping contexts: an MSBuild item list (`;` splits items), a C# string literal (`"` and `\` are compile
errors), and XML attribute values in the generated manifest (`&`, `<`, `>`). Plus `%` and `$` are MSBuild escape characters throughout. **One whitelist closes all of
them**, applied before any other derivation — keep only `A-Z a-z 0-9 space . _ -`.

### 3.2 The four values

| Value | Derivation | `worktree-identity` | `issue-36-spec-kit` | Consumers |
|---|---|---|---|---|
| **Id segment** | `wt` + 8 padded alnum + 6 hex | `wtworktree34acf2` | `wtissue36……` | `ApplicationId` |
| **Long tag** | sanitised name, capped at **21** chars | `worktree-identity` | `issue-36-spec-kit` | Windows `DisplayName`, window title, About card |
| **Short tag** | segment initials (≤4), else first 4 chars | `Iden` | `I36S` | Android label, iOS `CFBundleName`/`CFBundleDisplayName` |
| **Dev port** | `5001 + (hash % 999)` | — | — | WebAssembly dev server (§7.1) |

### 3.3 Exact algorithms

The survey's original snippet was **wrong** and must not be copied: `.PadRight(8,'0')` sat *outside* the `$(...)` property function and was appended as literal text, so
`ab` produced `wtab.PadRicde8b7` — a stray `.` is an MSIX/Android label separator, so it fails as a *wrong id* rather than loudly. Use the two-step form: compute the
sanitised alnum string, then pad it in a second property, then substring in a third.

Short tag — a single regex collapses each segment to its first character (`([A-Za-z0-9])[A-Za-z0-9]*[^A-Za-z0-9]*` → `$1`); when the result is shorter than 2 chars the
name was a single segment, so fall back to its first 4 characters. Cap at 4, Title-case.

**Hash algorithm must be pinned.** `[MSBuild]::StableStringHash(x)` (one-arg) has changed across MSBuild change waves — that is precisely why the two-arg overload
exists. Identity must be stable for the *life of the install*: a VS-msbuild vs `dotnet msbuild` difference, or an SDK upgrade, would orphan the app data. Use the
explicit `'Sha256'` overload and state the guarantee in the docs.

**Every algorithm in this section is asserted, not yet measured.** The single biggest lesson from the survey is that an unverified MSBuild snippet reads plausibly and
behaves differently. Implementation starts by evaluating each one against the fixture table in §8.1.

---

## 4. Safety model

The survey placed every guard on *detection* and gated the *apply* step only on "is the segment non-empty". That leaks, and it was reproduced:

- `-p:AppChannel=Prod -p:CI=true -p:EnableWorktreeIdentity=false -p:WorktreeName=oops` → a **Prod** id carrying a worktree suffix.
- `WorktreeName` exported as an environment variable → same result, silently, because MSBuild surfaces environment variables as properties.

Both paths are closed:

1. **All gates move onto the apply group.** A single derived `_WorktreeIdentityAllowed` flag combines `AppChannel == 'Dev'`, `CI != 'true'`,
   `ContinuousIntegrationBuild != 'true'` and `EnableWorktreeIdentity != 'false'`. Nothing downstream reads the worktree name without it.
2. **The public property is namespaced** to `AppWorktreeName`. `WorktreeName` is an un-namespaced, highly collidable environment-variable name; `AppWorktreeName` is not.
   The internal computed value stays `_`-prefixed and is never overridable.

### 4.1 Invariants

| # | Invariant | Enforced by |
|---|---|---|
| I1 | `AppChannel=Prod` never carries a worktree suffix, whatever is passed or exported | `_WorktreeIdentityAllowed` on the apply group |
| I2 | The main checkout produces byte-identical identity to today | `.git` is a directory → detection empty |
| I3 | CI produces byte-identical identity to today | real clone + explicit CI gates |
| I4 | `src/AppTemplate/Package.appxmanifest` is never written to | suffix applies to properties only |
| I5 | The id is always ≤ 50 chars, letter-first, lowercase alnum + dots | fixed-width 16-char segment |
| I6 | The Windows `ShortName` is always ≤ 40 chars | 21-char cap on the long tag |

I4 matters because `validate-manifest-version.yml` greps the tracked manifest for the literal `Version="0.0.0.0"` and `package-windows.yml` reads
`$xml.Package.Identity.Version` from it.

`ci.yml:57` is a bare `dotnet build` with no `AppChannel` and no `ContinuousIntegrationBuild`. Once the gates move onto the apply group this becomes **load-bearing
rather than incidental**, so `ContinuousIntegrationBuild: true` is added to `ci.yml` and `static-web-apps-deploy.yml` so all five workflows suppress by the same
explicit mechanism.

---

## 5. Length budgets

Every cap below is from the authoritative schema or Apple/Microsoft docs, not from folklore.

| Field | Limit | Source | Budget after fixed text |
|---|---|---|---|
| MSIX `Identity/@Name` | 3–50, `[-.A-Za-z0-9]+` | `AppxManifestTypes.xsd`, package-identity docs | `dev.mzikmund.apptemplate.dev` = 28 → **21** left; segment is 16 → total **45** |
| MSIX `uap:DefaultTile/@ShortName` | **40** (`ST_ShortDisplayName`) | `UapManifestSchema.xsd:99` | `App Template Dev (` + `)` = 19 → **21** for the name |
| MSIX `Properties/DisplayName` | 1–256 | packaging docs | ~30 before Start-menu truncation |
| iOS `CFBundleName` | **< 16** | Apple `CFBundleName` docs | `App Template` is already 12 → base must abbreviate to `AppTmpl` |
| Android `applicationId` segment | valid Java identifier, letter-first | aapt | the `wt` prefix guarantees it |

The `ShortName` cap is the one the original brief missed entirely: it bounded the *id* rigorously and left the *title* unbounded. `App Template Dev (issue-36-spec-kit)`
is 36 and fits; a 25-character admin dir would blow the cap and fail packaging. **The 21-char cap on the long tag is load-bearing, not cosmetic.**

The Android `wt` prefix also makes a Java reserved word (`int`, `new`, `do`) unreachable as a package segment. Stated here so a later "drop the redundant prefix"
refactor does not quietly reintroduce it.

---

## 6. Per-platform application

### 6.1 Windows, Desktop — properties only

A third `PropertyGroup` in `AppTemplate.csproj`, after the channel groups, gated on `_WorktreeIdentityAllowed`, appends the id segment to `ApplicationId` and the long
tag to `ApplicationTitle`.

`ApplicationPublisher` is **not** touched — it must keep matching the signing certificate subject used by `package-windows.yml`. Icons are not touched.

This gives Windows a distinct Package Family Name, AUMID, Start entry and `%LOCALAPPDATA%\Packages\<PFN>\`. It fixes, as a consequence,
`AppUpdater.EnsureAppUpToDateAsync()` cross-contamination and `SettingsViewModel.ClearPreferences` wiping the other worktree's store — both were only dangerous
because `ApplicationData.Current.LocalSettings` resolved to one shared container.

**Desktop must be verified, not assumed.** The survey claimed the Skia head's app-data folder is `%LOCALAPPDATA%\<publisher>\<Identity Name>\` and therefore isolated
"for free". The critique found that the observed folder on this machine is `dev.mzikmund.apptemplate` (the **Prod** id) with no `.dev` variant present, despite local
builds defaulting to `AppChannel=Dev` — so the evidence cited does not support the conclusion. Implementation **must** confirm with a fresh `-f net10.0-desktop` run
before ruling out `ApplicationDataPathOverride`.

### 6.2 Window title

`WindowShell.xaml.cs:UpdateWindowTitle()` sets the title from the *page* title (`App Template` / `Settings`), which is separate from the Start-menu name. It gains the
long tag so two running windows are distinguishable in the taskbar and Alt-Tab. This invalidates ~25 documented `winapp ui -a "App Template"` lines, which move to PID
targeting (§7.4) — a fix worth making regardless.

### 6.3 Android and iOS — localised resource overlay

The naive approach (replace `Label = "@string/ApplicationName"` with a generated `const`) **destroys localisation**. Both platforms localise the display name properly:

| Platform | Files | Current value |
|---|---|---|
| Android | `Resources/values/Strings.xml`, `Resources/values-cs/Strings.xml` | `App Template` |
| iOS | `Resources/en.lproj/InfoPlist.strings`, `Resources/cs.lproj/InfoPlist.strings` | `App Template` |

Instead, when and only when `_WorktreeIdentityAllowed`, a target reads each tracked resource file, appends ` [<ShortTag>]` to the name value **per locale**, writes the
result under `$(IntermediateOutputPath)`, and swaps the `@(AndroidResource)` / `@(BundleResource)` item to the generated copy. `Main.Android.cs` keeps
`Label = "@string/ApplicationName"`; translators keep editing the tracked files. The worktree tag is a proper noun, appended mechanically to every locale.

`Platforms/iOS/Info.plist`'s own `CFBundleName`/`CFBundleDisplayName` are the non-localised fallback and are patched consistently. Because `CFBundleName` must stay
under 16 characters, the suffixed form abbreviates the base: `AppTmpl [Iden]`, not `App Template [Iden]`.

**iOS device deploys need a wildcard App ID.** `CFBundleIdentifier` is filled from `$(ApplicationId)` because `Info.plist` declares no `CFBundleIdentifier` key — correct
for the **simulator**, but a per-worktree bundle id matches no existing provisioning profile. Device deployment requires a wildcard App ID with automatic signing. This
is a documented limitation, not a bug to fix.

---

## 7. WebAssembly, code generation, UI, tooling

### 7.1 Dev-server port

WASM has no install identity; isolation is the browser origin, and `launchSettings.json` pins `http://localhost:5000` (mirrored in `src/.vscode/launch.json` twice and
`src/.run/AppTemplate.run.xml`). Same port means both a bind failure *and* shared `localStorage` (Uno's keys are `UnoApplicationDataContainer_Local_*`, carrying no app id).

The port is derived as `5001 + (hash % 999)` → 5001–5999, deliberately excluding 5000 so the main checkout keeps it. It is injected via an MSBuild property so **no
tracked file is edited** — editing `launchSettings.json` per worktree branch reproduces exactly the recurring-merge-conflict pattern `.claude/rules/docs.md` exists to prevent.

**This needs a spike.** Whether an MSBuild property actually reaches the Uno WASM dev server is unverified. If it does not, the fallback is to log the derived port at
build time and document `--urls`. The Uno Dev Server / Hot Design port (`MainWindow.UseStudio()`, `App.xaml.cs:67`, `#if DEBUG`, runs on *every* head) is likewise
unmeasured under two concurrent worktrees and is part of the same spike.

### 7.2 Reaching C#

`AppEnvironment` can carry an `#if APP_CHANNEL_DEV` flag but not a string. A target writes `AppEnvironment.Worktree.g.cs` into `$(IntermediateOutputPath)` and adds it
to `@(Compile)`; `AppEnvironment` becomes `partial`. Chosen over NBGV `AdditionalThisAssemblyFields` (which would need declaring in *both* projects, since
`ThisAssembly` is `internal` and per-assembly) and over `AssemblyMetadataAttribute` (a trimming-fragile reflection read on the About page).

**The up-to-date check must include the value, not just project timestamps.** `Inputs="$(MSBuildAllProjects)"` alone means the target is *skipped* whenever no project
file is newer — but `AppWorktreeName` can change without touching any project file (`git worktree move`, or a command-line override). `WriteOnlyWhenDifferent` cannot
help, because the task never runs. The stale constant then survives in `obj/` and the About card lies about which package is installed — the exact failure the "extend
the same chain so About can never disagree" argument exists to prevent. Fold the name into the `Outputs` path, or drop `Inputs`/`Outputs` and rely on
`WriteOnlyWhenDifferent` alone.

### 7.3 About card

`IApplication` gains `string? WorktreeName`, flowing `App.xaml.cs` → `SettingsViewModel.WorktreeLabel` → a secondary caption `TextBlock` under the version in the About
`SettingsCard`. `StringVisibilityConverter` is already registered in `Resources/Converters.xaml`, so no new converter is needed. Format string `WorktreeFormat` goes in
**both** `Strings/en/Resources.resw` and `Strings/cs/Resources.resw` — a key present in only one renders as `???WorktreeFormat???`. The displayed value is the raw
admin-dir name, never the mangled id slug.

Widening `IApplication` is preferred over a new `IBuildInfo`: it is the seam head-level identity already crosses, and the alternative adds a second registration and a
second fake for a one-string, dev-only concept. Note that **no `IApplication` fake exists yet** — one is created.

The `DEV` badge is **not** extended. `Controls/DevChannelBadge.xaml` sits `HorizontalAlignment="Right" VerticalAlignment="Top"` with `Grid.RowSpan="2"` and
`WindowShell.xaml.cs` sets `ExtendsContentIntoTitleBar = true`, so that corner is the caption-button area. Widening `DEV` to `DEV · worktree-identity` walks it under the
close button.

### 7.4 Documentation and skill changes

`winapp ui status --help` documents `-a` as "process name, window title, or PID — lists windows if ambiguous" and `-w <HWND>` as taking precedence. So the fix is to
capture the `ProcessId` that `winapp run --detach --json` already returns and pass `-a <pid>`.

| File | Change |
|---|---|
| `.claude/skills/run-winui-app/SKILL.md` | ~19 `-a "App Template"` lines → PID targeting; lines 59/71 stop quoting a literal `dev.mzikmund.apptemplate.dev` id as fact; note `$out` resolves against the caller's cwd; resolve the dangling `docs/winui-run-notes.md` reference (line 218, file does not exist) |
| `AGENTS.md` (~40–60) | same loop, including the `-a is the window TITLE` comment |
| `docs/building.md` (45–60) | third prose copy — make two of the three link to the third rather than restating; three copies is how the drift happened |
| `docs/worktree-identity.md` | **new**, per `.claude/rules/docs.md` |
| `docs/README.md` | exactly one line, under `## Building & tooling`, alphabetically between `spec-kit.md` and `xaml-styler.md` |
| `docs/versioning.md` | add the worktree axis as a short linking section; **correct two errors** — lines 21 and 90 both claim the Windows Identity Name comes from the signing certificate's Publisher CN. The generated manifest shows the Name is `$(ApplicationId)` and the Publisher is `O=$(ApplicationPublisher)` |
| `docs/versioning-migration.md` | one step covering the detection block and the apply group |
| `README.md` | at most a link on the existing **Side-by-side Dev builds** row. No new prose |

Teardown guidance changes too: `Get-Process AppTemplate | Stop-Process -Force` is machine-wide and kills other worktrees; the `winapp run --json` PID is the thing to
stop. `winapp unregister --manifest` is now correctly per-worktree because it reads the generated manifest's identity.

The pre-existing profile-name mismatch (`src/.vscode/launch.json` and `src/.run/AppTemplate.run.xml` say `AppTemplate (Desktop)` while `launchSettings.json` defines
`App Template (…)`) is left alone — folding it in makes the diff look like worktree fallout.

---

## 8. Testing

Per `.claude/rules/testing.md`: failing test first, MSTest on MTP, FluentAssertions, hand-written fakes.

### 8.1 MSBuild fixture table

Evaluated with `dotnet msbuild` before any C# is written. Every row asserts the id is ≤50 chars, letter-first, lowercase alnum-plus-dots, and contains no `.` inside the
segment.

| `AppWorktreeName` | Long tag | Short tag | Notes |
|---|---|---|---|
| `worktree-identity` | `worktree-identity` | `Iden` | the ordinary case |
| `issue-36-spec-kit` | `issue-36-spec-kit` | `I36S` | digits in segments |
| `ab` | `ab` | `Ab` | **shorter than the 8-char pad** — the bug that broke the original snippet |
| `feature` | `feature` | `Feat` | 7 chars, single segment, also under the pad |
| `---` | *(empty)* | *(fallback)* | must not produce a stray `.` or an empty segment |
| `a-very-long-worktree-name-exceeding-limits` | truncated to 21 | 4 chars | `ShortName` cap |
| `name;with"quotes&` | sanitised | sanitised | item-list, C#-literal and XML escaping in one row |

Plus no-op assertions: main checkout; `-p:AppChannel=Prod`; and each leak vector from §4 (`-p:AppWorktreeName=…` combined with Prod, `AppWorktreeName` as an
environment variable, and `CI=true`).

### 8.2 Core unit tests

`FakeApplication : IApplication` in `tests/AppTemplate.Core.Tests` (house style — see `StubServiceProvider` in `Infrastructure/IoCTests.cs`):

- `WorktreeLabel_WhenNotInWorktree_ReturnsNull`
- `WorktreeLabel_WhenInWorktree_IncludesName`
- `WorktreeLabel_UsesLocalizedFormatString`

`src/Directory.Build.props` is imported only by projects under `src/`, not `tests/`, so the generated constant is invisible to the test project — which is exactly why
the `IApplication` seam exists rather than testing `AppEnvironment` directly.

### 8.3 End-to-end

Build and launch the Windows head from **both** worktrees simultaneously via the `run-winui-app` skill; confirm two Start entries, two distinct PFNs, two windows with
distinct titles, and that unregistering one leaves the other running.

---

## 9. Known limitations (to document, not fix)

- `git worktree move` renames the admin dir → new identity, orphaned install and app data.
- Every worktree accumulates a registered MSIX package. `Get-AppxPackage dev.mzikmund.apptemplate.dev.wt*` finds strays; there is no automatic cleanup.
- A deliberate local `-p:AppChannel=Prod` build from a worktree produces the **canonical Prod identity** and will overwrite a real installed Prod app. This is the safe
  direction (no suffixed id can ever ship) but deserves a sentence.
- App icons are identical across worktrees (§1.2).
- `Get-Process AppTemplate` still matches every worktree (§1.2).
- The Desktop head's `<Identity Version>` stays `1.0.0.1` because `SetNbgvVersionForUnoWindows` is gated on `TargetPlatformIdentifier == 'windows'`. The About version
  string is therefore **already wrong on Desktop**, and the new worktree line will sit right next to it. Pre-existing and out of scope — but it will read as a regression
  once someone sees the two lines together. Fix in a separate commit or note it.
- Forward-looking rules, cheapest to write down while the surfaces are still empty: a future SQLite database must be rooted at `ApplicationData.Current.LocalFolder`, and
  a future toast COM CLSID or `apptemplate://` scheme is a **machine-global** registration that `ApplicationId` isolation does not cover.

---

## 10. What we deliberately did NOT do

| Rejected | Why |
|---|---|
| Branch-derived identity | Churns on every branch switch, orphaning installs mid-session |
| Path-hash-only identity (`wt7a3f`) | Unreadable; the whole point is telling worktrees apart at a glance |
| A third `AppChannel` value | `ApplicationId`/`ApplicationTitle` have no unconditional fallback; a third value leaves both unset |
| `git` via `Exec` | Cannot run at evaluation time, where identity is decided; ~78 ms × 11 project instances |
| Per-worktree edits to `launchSettings.json` | Tracked file — reproduces the recurring-merge-conflict pattern `.claude/rules/docs.md` prevents |
| Generated `const` for the Android label | Destroys the `values-cs` localisation |
| Extending the `DEV` badge | Walks under the caption buttons |
| Renaming `AssemblyName` | Breaks launch configs, the linker config, and `--exe` |

---

## 11. Open questions for the implementation plan

1. Does an MSBuild property actually reach the Uno WASM dev server, or is `--urls` the only lever? (§7.1 spike)
2. Does the Skia Desktop head genuinely derive its app-data folder from `ApplicationId`? (§6.1 — the survey's evidence contradicted its own conclusion)
3. Does `[MSBuild]::StableStringHash(x, 'Sha256')` return hex directly, and is a 6-character substring safe on its output?
4. Do two concurrent Uno Dev Server / Hot Design instances contend for a port? (§7.1)
5. Does swapping `@(AndroidResource)` / `@(BundleResource)` items to generated copies work cleanly with Uno.Sdk's resource pipeline, or does aapt see duplicates?
