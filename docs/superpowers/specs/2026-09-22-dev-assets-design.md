# Generated Dev-channel icon and splash — design

**Date:** 2026-09-22
**Status:** Proposed
**Builds on:** [`2026-05-28-versioning-redesign-design.md`](2026-05-28-versioning-redesign-design.md) (the `AppChannel` model) and
[`2026-09-06-worktree-identity-design.md`](2026-09-06-worktree-identity-design.md) (the in-app `DevChannelBadge`).
**Applies to:** `uno-app-template` and every app created from it.

---

## 1. Model

> **A Dev-channel build takes the prod icon foreground and splash SVGs, stamps a top-right "DEV" badge on them at build time, and hands the
> result to Uno.Resizetizer. An adopter supplies only the prod artwork. There is no Dev artwork to draw, commit, or keep in sync.**

Today the Dev icon is a hand-recoloured copy of the logo (`Assets/Icons/icon_foreground_dev.svg`, selected by one channel-conditional
`UnoIconForegroundFile` line in `AppTemplate.csproj`). Anyone adopting the template has to redraw it for their own logo, and it drifts silently
whenever the prod icon changes. The splash screen has no Dev variant at all.

### 1.1 Goals

- Replacing `icon_foreground.svg` and `splash_screen.svg` is the whole job. The Dev variants follow on the next build.
- The Dev badge on the icon matches the in-app `DevChannelBadge` in shape and proportion.
- The badge stays fully visible under every platform mask it meets (Android adaptive icons, the Android 12+ splash, iOS icons).
- Prod builds are byte-for-byte unaffected.
- No new tool on the build machine. Builds work on Windows, macOS, and Linux with the stock .NET SDK or Visual Studio MSBuild.

### 1.2 Non-goals

- **Worktree name on the icon.** Explored and rejected: at the 16–32px taskbar sizes the name is 1–3px tall, and the Android circle mask cuts
  it off. Worktrees keep showing their name as text in the title bar, Start menu, and About (see `docs/worktree-identity.md`).
- **Theme-aware colours.** An app icon cannot follow the theme, so the badge uses one fixed colour (§3.1) rather than `SystemFillColorCautionBrush`.
- **The title-bar icon.** `WindowShell.xaml` keeps `icon_foreground.png` (the prod logo); the `DevChannelBadge` right beside it already says DEV.
- **Changing Prod artwork** in any way.

---

## 2. Build flow

```
evaluation (unchanged)                 GenerateDevAssets (Dev only)                  Uno.Resizetizer (unchanged)
─────────────────────                  ───────────────────────────                  ───────────────────────────
UnoIcon         fg = icon_fg.svg  ──▶  compose + write obj/…/devassets/<hash>/  ──▶  UnoResizetizeCollectItems
UnoSplashScreen    = splash.svg        icon_foreground.svg, splash_screen.svg        UnoResizetizeImages
                                       re-point the two items at them
```

1. **Evaluation is untouched.** `UnoIconForegroundFile` and `UnoSplashScreenFile` keep pointing at the prod SVGs. Uno.Sdk creates its items only
   when those files exist at evaluation time (`Uno.DefaultItems.Resizetizer.targets`: `Condition="Exists(...)"`), so a file generated during
   the build could never be named there. Leaving the properties alone sidesteps that entirely.
2. **`GenerateDevAssets`** runs with `BeforeTargets="UnoResizetizeCollectItems"`. For each of the two items it calls the composer (§3) and gets
   back the path of the badged copy.
3. It **re-points the items**:
   - `UnoIcon`: `<UnoIcon Update="@(UnoIcon)" ForegroundFile="…" />`. The item spec (the background) is unchanged.
   - `UnoSplashScreen`: the item spec *is* the file, so the item is replaced by a transform (`@(UnoSplashScreen->'…')`). A transform keeps the
     `BaseSize` / `Color` / `Scale` metadata.
4. Resizetizer runs as it does today.

### 2.1 Why `<hash>/<original filename>`

- **The hash is in the path because Resizetizer won't notice a changed file at the same path.** `UnoResizetizeImages` lists `@(UnoImage)`
  (whose item spec for the app icon is the *background*) and `UnoImage.inputs` in its `Inputs`. The inputs file records the foreground's
  **path**, not its timestamp. The default layout only regenerates on a foreground edit by accident, because Uno.Sdk's `Assets/**/*.svg` glob
  also includes the foreground as a plain `UnoImage`. A file under `obj/` gets no such help. A new folder per content hash changes the recorded
  path, which invalidates the target. (Suspected upstream issue, recorded for filing.)
- **The filename is kept because resource names derive from it.** The splash output name (`uno_splash_image`, the Windows manifest splash
  entry) comes from the splash file name. Keeping `splash_screen.svg` and `icon_foreground.svg` as-is means every generated resource keeps the
  name it has today.
- `<hash>` is the first 12 hex characters of SHA-256 over the **composed output**. The composer is deterministic (§3.4), so an unchanged
  input writes nothing and triggers no Resizetizer work.

### 2.2 Gating

`GenerateDevAssets` runs when **all** of these hold:

- `'$(AppChannel)' == 'Dev'`.
- `'$(GenerateDevAssets)' != 'false'`.
- The project has an `UnoIcon` or `UnoSplashScreen` item (library projects are skipped).

It runs on **CI too**. Dev packages built from `main` carry the badge, just as they carry today's recoloured icon. That differs deliberately
from worktree identity, which is local-only.

### 2.3 Opting out, and a hand-made Dev icon

- `-p:GenerateDevAssets=false` (or the property in the csproj) hands Resizetizer the prod artwork untouched.
- A hand-drawn Dev icon works the same way it does today: turn generation off and add the channel-conditional `UnoIconForegroundFile` line.
  With generation left **on**, a Dev-specific foreground gets badged too. That's documented, not special-cased.

---

## 3. The composer

One C# file, `src/DevAssets.Task.cs`, holds a pure composer plus a thin MSBuild task wrapper. `src/DevAssets.targets` loads it through
`RoslynCodeTaskFactory` (`<Code Type="Class" Source="DevAssets.Task.cs" />`). The test project compiles the same file (§6).

`RoslynCodeTaskFactory` compiles against `netstandard2.0` under Visual Studio's .NET Framework MSBuild, so this file is **conservative C#**:
no file-scoped namespace, no collection expressions, no records. That's a deliberate exception to `.claude/rules/code-style.md`, noted in a
one-line comment at the top of the file.

### 3.1 The badge

The badge copies the in-app `DevChannelBadge` (`Padding="6,2"`, 10px SemiBold text, `CornerRadius="4"`, about 17.3px tall), scaled so its
height is **24% of the image**. All values are percentages of the image's shorter side:

| Property | Value | Derivation |
|---|---|---|
| Height | 24 | chosen in the playground |
| Corner radius | 5.52 | 4 / 17.3 × 24 (≈ 46% of half-height) |
| Horizontal padding | 8.5 | 6 / 17.3 × 24, rounded |
| Text size (em) | 13.92 | 10 / 17.3 × 24 (58% of badge height) |
| Width | text advance + 2 × padding | from the outline's advance width |
| Fill | `#FFB900` | reads on dark and light taskbars |
| Text | `#141414` | |
| Anchor | top-right corner, flush | before the mask pull (§3.3) |

**"DEV" is drawn as outlines, not `<text>`.** The label never changes, so pre-drawn paths avoid any dependence on the fonts installed on the
build machine. The Android and iOS CI runners (Linux, macOS) don't have Segoe UI. The outlines come from **Selawik SemiBold**, Microsoft's
open-source (OFL-1.1) metric-compatible stand-in for Segoe UI. They're extracted once with fontTools and stored as a constant together with
their advance width and cap height. A source comment records the font, version, and licence. Segoe UI's own licence doesn't permit
redistributing its outlines in an MIT template.

### 3.2 Composition

`Compose(string svg, BadgePlacement placement) → string`:

1. Parse with `XDocument`. Read the root's `viewBox`, or fall back to numeric `width`/`height` (a `px` suffix is accepted). If neither
   is usable, the composer throws (the task turns that into a warning, §4).
2. Emit a new root with the **same** `viewBox`, `width` and `height`. Inside it:
   - the **original root element, unchanged**, nested as an `<svg>` child with `x=0 y=0 width=100% height=100%`. Its namespace
     declarations, `<defs>`, ids, and `preserveAspectRatio` travel with it.
   - one `<g>` holding the badge `<rect>` and the "DEV" `<path>`s, positioned in viewBox units.
3. Serialise with invariant-culture numbers (≤ 3 decimals) and stable attribute order.

Nesting leaves the original drawing intact, whatever it contains. **Fallback, if the spike (§7) shows Svg.Skia can't render a nested `<svg>`:**
append the badge `<g>` as the last child of the original root, in its viewBox coordinates.

For a non-square viewBox, the badge is sized from the shorter side and anchored to the viewBox's top-right corner.

### 3.3 Staying inside the mask

`BadgePlacement` = `{ Mask, Scale }`. The task derives it from the target platform and the item's own metadata, never from hard-coded offsets.
If the badge isn't fully visible under the mask, it's pulled **diagonally toward the centre** in 0.25% steps until it is. The pull is based on
the rounded corners' real arcs, with a 1% margin.

| Image | `Mask` | Visible region, in the composed SVG's coordinates |
|---|---|---|
| Icon on Windows / Desktop / WASM | `None` | whole canvas: badge stays flush |
| Icon on Android | `AndroidAdaptive` | circle at the centre, radius `min(50, 50 × (66/108) / ForegroundScale)`: the 66dp safe zone of the 108dp layer, which every launcher mask contains |
| Icon on iOS | `IosIcon` | canvas minus the corner arcs of a rounded rect with radius 22.37% of the icon, mapped through `ForegroundScale` |
| Splash on Android | `AndroidSplash` | the Android 12+ splash circle (192dp of the 288dp icon, so ⅓ radius), mapped through the splash `Scale` (verified in the spike) |
| Splash elsewhere | `None` | whole canvas |

With today's settings (`UnoIconForegroundScale` 0.6 on Android, 1 elsewhere), the playground measured the pull at **≈ 19.5% on Android** and
**≈ 8% on iOS**.

### 3.4 Determinism

The same input SVG and placement always produce the same bytes. The output has no timestamps, no GUIDs, and no culture-dependent formatting.
That's what makes the content hash (§2.1) stable across builds and machines.

---

## 4. Failure handling

A cosmetic badge must never break a build.

| Situation | Behaviour |
|---|---|
| Source SVG missing or not parseable | `warning DEVASSETS001` naming the file; the item keeps pointing at the prod artwork |
| No usable `viewBox` / `width` / `height` | same warning, same fallback |
| Mask pull fails to converge (badge larger than the visible region) | `warning DEVASSETS002`; badge emitted at its last position |
| Output directory not writable | MSBuild's normal error, same as any other write under `obj/` |

---

## 5. Files

| File | Change |
|---|---|
| `src/DevAssets.targets` | **new**: `UsingTask` + `GenerateDevAssets` target + gating |
| `src/DevAssets.Task.cs` | **new**: composer, mask geometry, "DEV" outlines, task wrapper |
| `src/Directory.Build.targets` | import `DevAssets.targets` |
| `src/AppTemplate/AppTemplate.csproj` | remove the Dev `UnoIconForegroundFile` line and its comment |
| `src/AppTemplate/Assets/Icons/icon_foreground_dev.svg` | **delete** |
| `tests/AppTemplate.Core.Tests/…` | link `DevAssets.Task.cs`; add `Microsoft.Build.Utilities.Core` (explicit version: tests sit outside CPM); tests under `DevAssets/` |
| `scripts/verify-dev-assets.ps1` | **new**: build-level checks (§6.2) |
| `.github/workflows/template-selftest.yml` | run the new script when the DevAssets files change |
| `docs/dev-assets.md` | **new**: what you see, how it works, opting out |
| `docs/README.md` | index line |
| `docs/versioning.md`, `docs/versioning-migration.md` | the Dev icon is generated; migration step 5 becomes "nothing to do" |
| `docs/worktree-identity.md` | update the "Icons are identical across worktrees" limitation wording |
| `README.md` | step 4, "Replace the artwork": no Dev icon to draw any more |

---

## 6. Testing

### 6.1 Unit tests (TDD, `AppTemplate.Core.Tests/DevAssets/`)

Written first. They cover:

- The original root survives verbatim inside the output (namespaces, `<defs>`, ids).
- The badge sits flush top-right in viewBox units, for viewBoxes starting at the origin **and** elsewhere (e.g. `-10 -10 120 120`).
- No `viewBox`: `width`/`height` (with and without `px`) are used. Non-numeric sizes throw.
- Non-square viewBox: sized from the shorter side, anchored top-right.
- For each `Mask` at scales 0.6, 0.65, 0.8 and 1.0, all four corner arcs of the badge lie inside the visible region.
- `None` never moves the badge. `AndroidAdaptive` at 0.6 and `IosIcon` at 1.0 pull by a non-zero amount. Exact values are pinned once the
  Selawik advance width is known; the playground's ≈ 19.5% / ≈ 8% came from Segoe UI metrics.
- Determinism: composing twice gives identical strings, and the output doesn't change with `CurrentCulture` (run under `cs-CZ`).
- Task wrapper: the output path is `<dir>/<12-hex>/<source filename>`, an existing file isn't rewritten, and a missing source logs
  `DEVASSETS001` and returns the source path.

### 6.2 Build verification (`scripts/verify-dev-assets.ps1`)

Written in the same style as `verify-worktree-identity.ps1`. It checks:

| # | Guarantee |
|---|---|
| D1 | A Dev build of the Windows head produces badged icon and splash PNGs (pixel sample in the top-right is `#FFB900`) |
| D2 | A Prod build creates no `devassets` folder, and its icon and splash PNGs are byte-identical to a Dev build with `GenerateDevAssets=false` (the artwork is untouched; only identity differs) |
| D3 | Editing the prod `icon_foreground.svg` regenerates the Dev icon on an **incremental** build |
| D4 | The generated resource names match a `GenerateDevAssets=false` build (no renamed PNGs, manifest entries unchanged) |
| D5 | The Android head builds and still resolves `@mipmap/icon` and `@drawable/uno_splash_image` |
| D6 | `-p:GenerateDevAssets=false` on the Dev channel yields the prod artwork |

### 6.3 Seeing it

Before calling it done: build the Windows and Android heads, look at the rendered PNGs, launch the packaged Windows app (`/run-winui-app`), and
check the Start menu / taskbar icon and the splash.

---

## 7. Spike first

These are risks that can't be settled by reading. They go first in the plan, as throwaway probes against a real build:

1. **Nested `<svg>`**: does Resizetizer's Svg.Skia render a nested root with its own `viewBox`? If not, use the §3.2 fallback.
2. **Item rewrite honoured**: does updating `UnoIcon.ForegroundFile` and replacing `UnoSplashScreen` in `BeforeTargets="UnoResizetizeCollectItems"`
   reach the rendered output on Windows **and** Android, with resource names unchanged?
3. **Hash-folder invalidation**: does a new `<hash>` folder re-run `UnoResizetizeImages` on an incremental build? (This also settles whether the
   suspected upstream issue in §2.1 is real.)
4. **Android splash geometry**: how Resizetizer applies `UnoSplashScreenScale` to `uno_splash_image`, so the `AndroidSplash` circle maps correctly.

---

## 8. Known limits

- **The iOS head can't be built on Windows.** The `IosIcon` placement is covered by unit tests only, not a real build, until someone checks it on
  a Mac. The same caveat already applies to the iOS label work in worktree identity.
- **At 16–24px the word "DEV" is illegible.** The gold corner still marks the build as Dev, the way a notification dot would. That's accepted,
  not a defect.
- **A badged hand-made Dev icon.** If an app sets its own Dev `UnoIconForegroundFile` without opting out, it gets badged too (§2.3).
