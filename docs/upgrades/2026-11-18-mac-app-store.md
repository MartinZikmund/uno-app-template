---
title: Publish the Desktop head to the Mac App Store
impact: optional
features: []
platforms: [desktop]
---

## Why

The template can now package the Desktop (Skia) head as a sandboxed, universal (Apple silicon and Intel) Mac app, sign it
for the Mac App Store and upload it from the release pipeline, as the macOS platform of the same App Store Connect app as
iOS. New apps get it with the `macAppStore` option (`--mac-app-store`), which is on by default for apps with the Desktop
platform.

The full guide is [mac-app-store.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/mac-app-store.md).

## Who is affected

Apps with the `desktop` platform that want to be on the Mac App Store. Skip this otherwise.

## Steps

`mzuno-app upgrade --patch` brings the files in, because the option is new and takes its default. To leave the Mac App
Store out after a patch, delete the files listed in step 1, undo the changes from steps 2 to 4, and set
`"macAppStore": "false"` in `.template-state.json`.

By hand, from the template at https://github.com/MartinZikmund/uno-app-template/tree/main:

1. Copy these, and rename `AppTemplate` in them to your app's name:
   - `src/AppTemplate/Platforms/Desktop/macOS/` (`Info.plist`, `Entitlements.plist`, `AppIcon/`)
   - `build/macos/package-app-store.sh` and `build/macos/export-icon.cs`
   - `.github/workflows/_build-macos.yml`, if the app has the release workflows
   - `.github/scripts/asc_op.py`, if the app has no iOS head and so doesn't have it yet
   - `tests/AppTemplate.Core.Tests/Packaging/MacBundleTests.cs`, if the app has the unit tests

   Then edit `Info.plist`: pick your App Store category, list the languages in `Strings/`, and fix the copyright. In
   `Entitlements.plist`, drop `com.apple.security.network.client` if the app makes no network calls.

2. Render your icon into `AppIcon/` with the layers your project's `UnoIcon` uses:

   ```bash
   dotnet run build/macos/export-icon.cs -- src/AppTemplate/Platforms/Desktop/macOS/AppIcon \
     src/AppTemplate/Assets/Icons/icon.svg src/AppTemplate/Assets/Icons/icon_foreground.svg 0.65
   ```

3. In `src/AppTemplate/AppTemplate.csproj`, before the first `<ItemGroup>` with package references:

   ```xml
   <!-- The Mac App Store bundle. See docs/mac-app-store.md. -->
   <PropertyGroup Condition="'$(IsDesktop)' == 'true'">
       <UnoMacOSCustomInfoPlist>$(MSBuildThisFileDirectory)Platforms/Desktop/macOS/Info.plist</UnoMacOSCustomInfoPlist>
       <UnoMacOSMinimumSystemVersion>12.0</UnoMacOSMinimumSystemVersion>
   </PropertyGroup>

   <PropertyGroup>
       <!-- Only the bundle's icon.icns uses these. build/macos/export-icon.cs renders them. -->
       <DefaultItemExcludes>$(DefaultItemExcludes);Platforms/Desktop/macOS/AppIcon/**</DefaultItemExcludes>
   </PropertyGroup>

   <!-- Uno builds icon.icns only from images named icon16x16@1x and so on, and the desktop icon has other names.
        https://github.com/unoplatform/uno/issues/24661 -->
   <Target Name="AddMacOSAppIcon" Condition="'$(IsDesktop)' == 'true'" BeforeTargets="_UnoPublishAppBundle">
       <ItemGroup>
           <_UnoResizetizerCollectedImages Include="$(MSBuildThisFileDirectory)Platforms/Desktop/macOS/AppIcon/*.png" />
       </ItemGroup>
   </Target>
   ```

4. In `src/Directory.Build.targets`, after the `SetNbgvVersionForUnoWindows` target, so the bundle gets the real version:

   ```xml
   <Target Name="SetMacOSBundleVersion"
           Condition="'$(IsDesktop)' == 'true'"
           BeforeTargets="_UnoPublishAppBundle"
           DependsOnTargets="GetBuildVersion">
       <PropertyGroup>
           <ApplicationDisplayVersion Condition="'$(CiVersionInjected)' != 'true'">$(BuildVersionSimple)</ApplicationDisplayVersion>
           <ApplicationVersion>$(ApplicationDisplayVersion)</ApplicationVersion>
       </PropertyGroup>
   </Target>
   ```

5. If the app has the release workflows, add the `build-macos` job to `build-main.yml`, and `build-macos`,
   `publish-mac-testflight` and `submit-mac-appstore` to `release.yml`, with `build-macos` and `publish-mac-testflight`
   in the `tag` job's `needs`. Copy them from the template's files. The `macappstore-*` actions in `store-ops.yml` and the
   macOS probe in `store-health.yml` are optional.

6. **Outside the code**, which no patch can deliver. All of it is in
   [mac-app-store.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/mac-app-store.md#one-time-apple-setup):
   - add the macOS platform to the app in App Store Connect,
   - create a Mac Installer Distribution certificate (you likely have Apple Distribution from iOS already),
   - create a Mac App Store Connect provisioning profile for the Prod bundle id,
   - set the `APPLE_MAC_INSTALLER_P12_BASE64` and `APPLE_MACOS_PROVISIONING_PROFILE_BASE64` secrets, plus
     `APPLE_DISTRIBUTION_P12_BASE64`, `APPLE_P12_PASSWORD` and the `APPSTORE_*` secrets if iOS hasn't set them. Until
     they're set, CI builds an unsigned bundle and the Mac App Store jobs skip green.

## Verify

On a Mac, `bash build/macos/package-app-store.sh` writes `artifacts/macos/AppTemplate.app.zip`, a universal bundle whose
`Info.plist` has your version. With the profile as its argument and both certificates in the keychain, it writes a signed
`artifacts/macos/AppTemplate.pkg`, and `xcrun altool --validate-app -t macos -f artifacts/macos/AppTemplate.pkg` passes.
