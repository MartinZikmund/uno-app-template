# Generated Dev icon and splash

Replace `src/AppTemplate/Assets/Icons/icon_foreground.svg` and `src/AppTemplate/Assets/Splash/splash_screen.svg` with your
own artwork, and you're done. Every Dev-channel build stamps a **DEV** badge onto both, and Prod builds use them untouched.
There is no Dev artwork to draw, commit, or keep in sync.

## What you see

The badge copies the in-app `DevChannelBadge` from the title bar: a gold (`#FFB900`) pill with dark "DEV" lettering,
24% of the image tall, in the top-right corner.

| Where | Dev build |
|---|---|
| Windows Start menu, taskbar, Alt-Tab | flush in the top-right corner |
| Desktop window icon, WebAssembly favicon | flush in the top-right corner |
| Android launcher | pulled towards the centre, into the 66dp safe zone that every launcher mask keeps |
| Android 12+ splash | pulled inside the circle the system crops the splash icon to |
| iOS home screen | pulled clear of the rounded corner |
| Splash on Windows, Desktop, WebAssembly, iOS | flush in the top-right corner of the logo |

At 16–24px the word is unreadable, but the gold corner still marks the build, the way a notification dot does.

The worktree name is deliberately **not** on the icon: at taskbar sizes it would be 1–3px tall. Worktrees show their name
as text instead. See [worktree-identity.md](./worktree-identity.md).

## How it works

[`src/DevAssets.targets`](../src/DevAssets.targets) runs `GenerateDevAssets` just before Uno.Resizetizer collects its
items. It runs on the Dev channel only, **including CI**, so Dev packages built from `main` carry the badge.

1. It takes the `UnoIcon` foreground and the `UnoSplashScreen` image exactly as Uno.Sdk created them from
   `UnoIconForegroundFile` and `UnoSplashScreenFile`.
2. `ComposeDevAsset` ([`src/DevAssets.Task.cs`](../src/DevAssets.Task.cs)) nests each SVG unchanged inside a wrapper and
   draws the badge on top. MSBuild's `RoslynCodeTaskFactory` compiles it, so no extra tool is needed.
3. The result goes to `obj/<config>/<tfm>/devassets/<content hash>/<original file name>`, and the items are pointed at it.

Three details hold this together. Each works around something Resizetizer does:

- **The content hash is in the folder name** because Resizetizer doesn't notice when a file changes at the same path. It
  records the foreground's *path*, not its timestamp. A new folder is a new path.
- **The file name is unchanged** because Resizetizer derives resource names from it (`@drawable/splash_screen`, the
  Windows manifest's splash entry). Nothing downstream sees a difference.
- **Android icons are re-rendered when the foreground changes.** Resizetizer's adaptive-icon generator only regenerates
  when the foreground *file* is newer than its output. Switching Dev → Prod in the same `obj/` would otherwise keep the
  badge. `InvalidateStaleAndroidAppIcons` records the foreground and its scale, and clears the stale output when they change.

"DEV" is drawn with outlines from Selawik Semibold, Microsoft's open-source (SIL OFL 1.1) stand-in for Segoe UI. It
therefore looks the same whichever machine builds it; the Linux and macOS CI runners don't have Segoe UI.

The badge's position comes from each image's own scale metadata, read the way Resizetizer reads it:
`AndroidForegroundScale` over `ForegroundScale`, `AndroidScale` over `Scale`, and so on. Changing
`UnoIconForegroundScale` or `UnoSplashScreenScale` moves it correctly.

## Turning it off

| You want | Do this |
|---|---|
| The prod artwork on a Dev build | `dotnet build … -p:GenerateDevAssets=false`, or set the property in the csproj |
| A hand-drawn Dev icon | set `GenerateDevAssets` to `false` and add `<UnoIconForegroundFile Condition="'$(AppChannel)' == 'Dev'">Assets/Icons/my_dev_icon.svg</UnoIconForegroundFile>` |

With generation left on, a Dev-specific foreground gets badged too.

## Warnings

| Code | Meaning |
|---|---|
| `DEVASSETS001` | The SVG is missing, malformed, or has no usable `viewBox`, `width` or `height`. The build uses the prod artwork. |
| `DEVASSETS002` | The badge can't fit inside the platform mask at this scale (e.g. a very large `UnoIconForegroundScale`), so it may be clipped. |

A cosmetic badge never fails the build.

## Verifying

```powershell
dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"
pwsh scripts/verify-dev-assets.ps1                   # desktop head
pwsh scripts/verify-dev-assets.ps1 -IncludeAndroid   # adds D5; needs the Android workload
```

On the template repository, [`template-selftest.yml`](../.github/workflows/template-selftest.yml) runs both whenever the
Dev asset files or the artwork change.

| # | Guarantee |
|---|---|
| D1 | A Dev build badges the icon and splash, top-right, in `#FFB900` |
| D2 | A Prod build writes nothing under `devassets` |
| D3 | Editing the prod icon re-renders the Dev icon on an incremental build |
| D4 | Every generated resource keeps its name |
| D5 | Android badges the adaptive icon, still references `@drawable/splash_screen`, and goes back to the plain icon when you switch back |
| D6 | `GenerateDevAssets=false` renders exactly what Prod renders |

## Limitations

- **iOS is covered by unit tests only.** The iOS head can't be built on Windows. Resizetizer's Apple icon generator appears
  to share the Android generator's timestamp-only check, so if an iOS Prod build after a Dev build still shows the badge,
  delete that head's `obj/` folder.
- **The Android 12+ splash circle is derived, not observed.** It comes from Resizetizer's 108dp splash drawable and
  Android's 72dp visible circle. Confirm it on a device if you change `UnoSplashScreenScale` a lot.

## See also

- [versioning.md](./versioning.md): the Dev/Prod `AppChannel` model this hooks into.
- [worktree-identity.md](./worktree-identity.md): why the worktree name stays out of the icon.
- [Design spec](./superpowers/specs/2026-09-22-dev-assets-design.md): the decisions and the spike behind them.
