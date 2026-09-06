# Versioning

> `main` is where the next release is brewing; `release/v{minor}` branches are where releases ship.
> Every build carries an `AppChannel` (Dev for local + main, Prod for release/v*) baked into its package identity so Dev and Prod sit side-by-side on every device.

## Branch model

| Branch | `version.json` | NBGV `SimpleVersion` | Channel |
|---|---|---|---|
| `main` | next planned minor with a prerelease tag, e.g. `0.2-dev` | `0.2.{height}` | Dev |
| `release/v0.2` | `0.2` (written by `nbgv prepare-release`) | `0.2.0`, `0.2.1`, … | Prod |
| `feature/*` | inherited from main | `0.2.{height}` | Dev |

`publicReleaseRefSpec` matches `release/v{minor}` only, so `main` can never produce a
stable version by accident.

### `version.json` must carry a prerelease tag

`"version": "0.2-dev"`, **not** `"0.2"`. With a bare `"0.2"`, nbgv considers `main` to
already *be* a release version, so `nbgv prepare-release` has nothing to cut and returns
`"NewBranch": null` — the release cut silently has no branch to create. `prepare-release.yml`
preflights for exactly this and fails with an explanatory error.

The prerelease tag is what makes a cut meaningful; the major number is unrelated to it.
`0.2-dev` works exactly as well as `1.0-dev`.

## How a version reaches each store

The patch component **is the git height**, and the height does *not* reset when a release
branch is cut — `release/v0.2` inherits `main`'s accumulated height. Patch numbers are
therefore not contiguous (a README typo on a release branch burns one), which is fine:
monotonic is the only property stores actually enforce.

```
major, minor  <- version.json
patch         <- NBGV git height          (SimpleVersion == major.minor.patch)

simple  = "X.Y.Z"                              iOS CFBundleVersion + ShortVersionString
store4  = "X.Y.Z.0"                            Windows Identity/@Version
code    = X*10_000_000 + Y*100_000 + Z         android:versionCode
```

**Slot budget** — Google Play's ceiling is 2 100 000 000:

| slot | max | why |
|---|---|---|
| patch `Z` | 99 999 | 100 000 would carry into the minor slot |
| minor `Y` | 99 | 100 would carry into the major slot |
| major `X` | 209 | `209·10⁷ + 99·10⁵ + 99 999 = 2 099 999 999` ✅ |

Five digits of patch rather than three, because the height accumulates across release
branches; three digits would overflow within a few of them.

### Monotonicity, worked

| # | Event | `SimpleVersion` | Windows | `versionCode` | iOS |
|---|---|---|---|---|---|
| 1 | `main`, dev build | `0.2.1` | `0.2.1.0` | `200 001` | `0.2.1` |
| 2 | `main`, 40 commits later | `0.2.41` | `0.2.41.0` | `200 041` | `0.2.41` |
| 3 | **cut `release/v0.2`** | `0.2.42` | `0.2.42.0` | `200 042` | `0.2.42` |
| 4 | patch on `release/v0.2` | `0.2.43` | `0.2.43.0` | `200 043` | `0.2.43` |
| 5 | 3-commit fix | `0.2.46` | `0.2.46.0` | `200 046` | `0.2.46` |
| 6 | `main` now `0.3-dev` (height resets) | `0.3.1` | `0.3.1.0` | `300 001` | `0.3.1` |
| 7 | `main`, 60 commits later | `0.3.61` | `0.3.61.0` | `300 061` | `0.3.61` |
| 8 | **cut `release/v0.3`** | `0.3.62` | `0.3.62.0` | `300 062` | `0.3.62` |
| 9 | late patch on old `release/v0.2` | `0.2.49` | `0.2.49.0` | `200 049` | `0.2.49` |

*Within a train:* the height strictly increases along a branch, so every store's number
strictly increases (rows 3→4→5).

*Across trains:* a minor bump adds exactly 100 000 and `Z ≤ 99 999`, so the highest
possible code on `release/v0.2` (`299 999`) can never reach the lowest on `release/v0.3`
(`300 001`) no matter how long the old branch lives.

Row 9 is the one honest caveat, and it is a store rule rather than a formula defect: once
`0.3.62` is live, uploading `0.2.49` to the same track is rejected as lower. Patching an
already-superseded train is a business decision — see
[release-runbook.md](./release-runbook.md).

### Never use `nbgv get-version -v Version` for a store

It yields e.g. `0.2.2.52624`, whose fourth component is derived from the **commit hash** and
is not monotonic. The same applies to `AssemblyFileVersion`. Use `SimpleVersion`; the
`.github/actions/app-version` composite is the single place this is computed.

### Why CI injects the version instead of MSBuild computing it

`ApplicationVersion` and `ApplicationDisplayVersion` are resolved at **evaluation** time by
five different SDKs, while NBGV assigns the real version inside the `GetBuildVersion`
*target* — far too late to influence them. Command-line global properties win on all five
at once, so every packaging job passes:

```
-p:CiVersionInjected=true -p:ApplicationDisplayVersion=X.Y.Z -p:ApplicationVersion=<per-platform>
```

`src/Directory.Build.targets` adds an `AssertStoreVersionStamped` guard that fails a Prod CI
build which lost its injection, and the three store-bound heads then read the version back
out afterwards: iOS unzips the `.ipa` and reads `Info.plist`, Windows opens the
`.msixbundle` and parses its bundle manifest, and Android reads the generated
`AndroidManifest.xml` under `obj/`. Desktop and WASM carry no store version and are not
checked. A local
`dotnet build -f net10.0-android` still emits `versionCode=1` — that is expected and
harmless, because you never upload from a laptop.

## AppChannel

Single MSBuild property: `AppChannel = Dev | Prod`. Defaults to `Dev`. Every CI packaging
workflow sets it from the branch (`release/v*` → Prod, else Dev).

What changes per channel:

- **Identity:** Android `applicationId`, iOS Bundle ID — `…apptemplate` vs `…apptemplate.dev`.
  On Windows the *Store* identity is separate again — see [windows-packaging.md](./windows-packaging.md).
- **Display name:** `App Template` vs `App Template Dev`.
- **App icon:** Dev overrides the icon *foreground* only. Uno derives the generated icon
  resource name from the background file, so the background must stay constant across
  channels.
- **In-app banner:** a `DEV` corner badge when `AppEnvironment.IsDevChannel` is true.
- **Compile constant:** `APP_CHANNEL_DEV` on Dev builds.
- **Appsettings:** `appsettings.Dev.json`.

## The `Package.appxmanifest` 0.0.0.0 pin

The checked-in manifest is a *template*; Uno regenerates it into `obj/` at build time and
`SetNbgvVersionForUnoWindows` supplies the version. The `Version="0.0.0.0"` pin is
load-bearing, and `ci.yml`'s `guards` job fails the build if it ever changes.

## Release lifecycle

Cutting, patching, halting and the store-by-store constraints all live in
[release-runbook.md](./release-runbook.md). The workflow map is in
[release-pipeline.md](./release-pipeline.md).

### Major version bump

Set `version.json` to `1.0-dev` on `main` manually instead of running the cut workflow.
NBGV does not enforce a major-bump policy.
