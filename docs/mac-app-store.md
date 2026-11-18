# Mac App Store

You want the app on the Mac App Store. The Desktop (Skia) head is the Mac app: it's packaged as a sandboxed,
universal (Apple silicon and Intel) app bundle, signed for the App Store, and wrapped in an installer `.pkg`. It ships as
the **macOS platform of the same App Store Connect app as iOS**, so the name, privacy answers, pricing and the purchase are
shared (universal purchase).

| File | What it is |
|---|---|
| `src/AppTemplate/Platforms/Desktop/macOS/Info.plist` | Merged into the bundle's `Info.plist`: App Store category, languages, copyright, export compliance |
| `src/AppTemplate/Platforms/Desktop/macOS/Entitlements.plist` | The sandbox and what the app may do inside it |
| `src/AppTemplate/Platforms/Desktop/macOS/AppIcon/` | The Mac icon, rendered by `build/macos/export-icon.cs` |
| `build/macos/package-app-store.sh` | Builds, merges, signs and packages. Runs on a Mac, locally or on CI |
<!--#if (packaging)-->

`.github/workflows/_build-macos.yml` runs the script on CI. See [On CI](#on-ci).
<!--#endif-->

## Before the first build

1. **Pick a category.** Change `LSApplicationCategoryType` in `Info.plist` to one from
   [Apple's list](https://developer.apple.com/documentation/bundleresources/information-property-list/lsapplicationcategorytype).
   The App Store rejects a bundle without one.
2. **Check the entitlements.** The template asks for the sandbox, which the App Store requires, and JIT, which the .NET
   runtime needs. Apps with the HTTP client also get outgoing network connections. Add only what the app uses: App
   Review asks about each one. The file has the open and save panel entitlement commented out, for
   `FileOpenPicker` and `FileSavePicker`.
3. **Check export compliance.** `ITSAppUsesNonExemptEncryption` is `false`, which is right for an app that only uses HTTPS.
4. **Keep the languages in step.** `CFBundleLocalizations` lists the folders in `Strings/`. A unit test fails when they
   drift.
5. **Render the icon** whenever the app icon changes. It mirrors the project's `UnoIcon` layers on Apple's icon grid:

   ```bash
   dotnet run build/macos/export-icon.cs -- src/AppTemplate/Platforms/Desktop/macOS/AppIcon \
     src/AppTemplate/Assets/Icons/icon.svg src/AppTemplate/Assets/Icons/icon_foreground.svg 0.65
   ```

   The second argument is the background, an SVG or a colour such as `#4C7A56`. The last one scales the foreground.

## One-time Apple setup

All of this happens once, in the [Apple Developer portal](https://developer.apple.com/account) and in
[App Store Connect](https://appstoreconnect.apple.com). The Mac app uses the same bundle id as iOS: the Prod
`ApplicationId` in `src/AppTemplate/AppTemplate.csproj`.

1. **Add macOS to the app record.** In App Store Connect, open the app, choose **Add Platform** next to the platform
   name, and pick macOS. If there's no iOS app yet, create a new app with the macOS platform instead.
2. **Certificates.** You need two, made from a certificate signing request on a Mac:
   - **Apple Distribution**, which signs the app. If you ship iOS too, it's the same certificate.
   - **Mac Installer Distribution**, which signs the `.pkg`. Keychain Access shows it as
     "3rd Party Mac Developer Installer".
3. **Provisioning profile.** Under Profiles, create a **Mac App Store Connect** profile for the bundle id with the
   Apple Distribution certificate. Download the `.provisionprofile`.
4. **Test information.** TestFlight wants a beta description and a feedback email before it processes a build. Fill them
   in under TestFlight > Test Information.

## Build and upload from a Mac

With both certificates in a keychain:

```bash
CODESIGN_KEY="Apple Distribution: Your Name (TEAMID)" \
INSTALLER_KEY="3rd Party Mac Developer Installer: Your Name (TEAMID)" \
  bash build/macos/package-app-store.sh path/to/profile.provisionprofile
xcrun altool --validate-app -t macos -f artifacts/macos/AppTemplate.pkg --apiKey <key id> --apiIssuer <issuer id>
xcrun altool --upload-app -t macos -f artifacts/macos/AppTemplate.pkg --apiKey <key id> --apiIssuer <issuer id>
```

`--validate-app` runs App Store Connect's checks without uploading. Without a profile, the script stops at an unsigned
universal bundle in `artifacts/macos/AppTemplate.app.zip`, which is enough to check a build. An App Store-signed bundle
won't start outside the App Store, so try the app with `dotnet run -f net11.0-desktop` instead.

The script:

1. publishes the desktop head for `osx-arm64` and `osx-x64` as app bundles, in the Prod channel unless `APP_CHANNEL` says
   otherwise,
2. merges them into one universal bundle,
3. embeds the provisioning profile,
4. signs the bundle with `Entitlements.plist` plus the application and team identifiers from the profile,
5. wraps it in an installer package signed for the App Store.

The version is `X.Y.Z` from Nerdbank.GitVersioning, or `APP_VERSION` when set. CI sets it, the same version the other
stores get. See [versioning.md](./versioning.md).
<!--#if (packaging)-->

## On CI

| Branch | What `_build-macos.yml` produces | Where it goes |
|---|---|---|
| `main` | an unsigned universal `.app` zip, Dev channel | nowhere: the profile is for the Prod bundle id |
| `release/vX.Y` | a signed `.pkg`, Prod channel | App Store Connect automatically, then App Review after an approval |

The upload (`publish-mac-testflight`) and the review submission (`submit-mac-appstore`) use the iOS jobs' `testflight` and
`appstore-production` environments and their App Store Connect key, so a repository that ships iOS needs only the macOS
signing secrets on top.

| Secret | What it holds |
|---|---|
| `APPLE_DISTRIBUTION_P12_BASE64` | the Apple Distribution certificate and key, exported as `.p12`, base64. Shared with iOS |
| `APPLE_MAC_INSTALLER_P12_BASE64` | the Mac Installer Distribution certificate and key, `.p12`, base64 |
| `APPLE_P12_PASSWORD` | the password of both `.p12` files. Shared with iOS, so export both with it |
| `APPLE_MACOS_PROVISIONING_PROFILE_BASE64` | the Mac App Store profile, base64 |
| `APPLE_CODESIGN_KEY` | optional: the signing identity's name. Default `Apple Distribution` |

```bash
base64 -i AppStore.provisionprofile | gh secret set APPLE_MACOS_PROVISIONING_PROFILE_BASE64
base64 -i MacInstaller.p12 | gh secret set APPLE_MAC_INSTALLER_P12_BASE64
```

Missing any of the four required secrets builds the unsigned bundle instead, and the App Store jobs skip green. Set the
`ENABLE_MACOS` repository variable to `false` to skip the macOS build altogether: it runs on a macOS runner, which costs
more on a private repository.

The review submission needs an App Store version for macOS with the release's version number, as the iOS one does. The
`macappstore-*` actions in `store-ops.yml` pause or resume a phased release and cancel a review, and `store-health.yml`
reports a rejected macOS version.
<!--#endif-->

## Workarounds

These are all Uno Platform gaps. Each workaround links its issue, so it can go once that's fixed.

| What | Where | Issue |
|---|---|---|
| The bundle icon needs images named `icon16x16@1x` and so on | `AddMacOSAppIcon` in the project | [uno#24661](https://github.com/unoplatform/uno/issues/24661) |
| The bundle gets no version without NBGV's help | `SetMacOSBundleVersion` in `src/Directory.Build.targets` | [uno#24662](https://github.com/unoplatform/uno/issues/24662) |
| Publishing a multi-targeted project needs only the desktop target, self-contained at restore | the script | [uno#24663](https://github.com/unoplatform/uno/issues/24663) |
| The merge target is only imported while publishing, and breaks on spaces | the script | [uno#24665](https://github.com/unoplatform/uno/issues/24665) |
| A universal bundle can't load `ms-appx:///` images | the script links `Assets` into each architecture | [uno#24666](https://github.com/unoplatform/uno/issues/24666) |
| No App Store signing or packaging in Uno's tooling | the script | [uno#24668](https://github.com/unoplatform/uno/issues/24668) |
| The title bar follows the system appearance, not the app theme | `Infrastructure/MacAppearance.cs` | [uno#24667](https://github.com/unoplatform/uno/issues/24667) |
| A window backdrop leaves stale frames on screen | the shell sets no Mica on macOS | [uno#24495](https://github.com/unoplatform/uno/issues/24495) |
