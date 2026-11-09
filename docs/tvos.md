# tvOS

Run the app on Apple TV. `net10.0-tvos` is a regular head of `src/AppTemplate`, sharing its views,
view models and services with every other head. Its platform files live in
`src/AppTemplate/Platforms/tvOS`.

## Build and run

```bash
# Compiles the C# on any OS except Linux - handy for a quick check on Windows
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-tvos

# On a Mac: build and launch in the Apple TV simulator
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-tvos -t:Run -p:RuntimeIdentifier=tvossimulator-arm64
```

Because restore evaluates every target framework, the `tvos` workload is needed for all heads, not
just this one - see [building.md](./building.md#restore-workloads). A Windows build stops after compiling:
bundling, the asset catalog and AOT all need Xcode, so a green Windows build says nothing about them.

## Icon and launch screen

Uno.Resizetizer generates both from the same `UnoIcon` and `UnoSplashScreen` as the other heads:

- `Assets.xcassets/icon.brandassets`, holding the layered app icons and the Top Shelf images.
  `Info.plist` points at it through `XSAppIconAssets`, and the name comes from `UnoIconBackgroundFile`,
  so rename both together.
- A launch storyboard, referenced from the generated `UILaunchStoryboardName`.

Tune the sizing with `TvOSForegroundScale` and `TvOSScale`. The Top Shelf images are the logo on the
icon background, which is a placeholder: replace them with real artwork before shipping. The Dev
badge stays inside the icon's parallax safe zone - see [dev-assets.md](./dev-assets.md).

## What behaves differently on Apple TV

- **The app runs on the interpreter, not AOT.** With full AOT the native link fails on undefined
  P/Invoke symbols, including Windows-only ones inside Uno.UI and Silk.NET. `AppTemplate.csproj` sets
  `MtouchInterpreter=all`, as Uno's own tvOS head does, so expect slower managed code than on iOS.
- **Input is focus-driven.** People use the Siri Remote, not touch or a pointer, so check that every
  page can be reached and used with focus alone. The template's pages have not been reviewed for this
  yet, and the smoke test below only proves the app starts.
- **There is no share sheet.** `ShareService.ShareAsync` does nothing on tvOS.
- **Opening links doesn't work yet.** `Launcher.LaunchUriAsync` returns `false` on tvOS
  ([unoplatform/uno#24650](https://github.com/unoplatform/uno/issues/24650)), so `LauncherService` and
  the rating prompt's store link do nothing.
- **Local storage is not durable.** Apple only guarantees small key-value storage, which is what
  `ApplicationData.Current.LocalSettings` uses. Files can be purged by the system, so keep anything
  important in the cloud.
- **SQLite needs its own native library.** `SourceGear.sqlite3` ships no tvOS binaries, so `sqlite-net-e`
  would fail at runtime. The template doesn't call it, but a feature that does needs another provider
  on this head.
- **No OSLog logging provider.** `Uno.Extensions.Logging.OSLog` has no tvOS build, so the SDK leaves it
  out on this head.

Worktree and channel display names work as on iOS. See [worktree-identity.md](./worktree-identity.md).

## CI

`ci.yml` compiles the head on Windows for every PR, which is cheap. `_build-tvos.yml`, called from
`build-main.yml` and `release.yml`, does the real build on a Mac:

| Signing configured | Result |
|---|---|
| No | A simulator `.app`, an unsigned device build to catch native link failures, and a smoke test that boots an Apple TV simulator, launches the app, and uploads a screenshot |
| Yes | A signed `.ipa` |

Signing reuses the iOS certificate secrets and adds these, because tvOS provisioning profiles are
separate from iOS ones:

| Name | Kind | Used by |
|---|---|---|
| `APPLE_TVOS_PROVISIONING_PROFILE_BASE64` | secret | `release.yml` |
| `APPLE_TVOS_ADHOC_PROVISIONING_PROFILE_BASE64` | secret | `build-main.yml` |
| `APPLE_TVOS_PROVISIONING_PROFILE_NAME` | variable | both |

Publishing is not wired up: the `.ipa` is an artifact only and never reaches TestFlight or the App
Store. `release.yml` still builds it, and a failed tvOS build blocks the release tag, like any other head.

## Dropping tvOS

If your app has no use for Apple TV, remove `net10.0-tvos` from `AppTemplate.csproj` and
`AppTemplate.Core.csproj`, delete `Platforms/tvOS`, and remove its row from the `ci.yml` matrix.
To only stop packaging it, set the repository variable `ENABLE_TVOS` to `false`.
