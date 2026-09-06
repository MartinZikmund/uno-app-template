# Migrating an existing app to the new versioning model

This guide walks an existing Uno app (stopwatch, daily-dozen, …) through adopting the model described in [versioning.md](./versioning.md). Apply per-app, on the app's own schedule.

## Pre-flight

Check what's currently shipped. If `version.json` says `0.12` and the last Store/Play submission was `0.12.x`, that's your current minor.

## Steps

### 1. Cut a release branch from current main HEAD

```pwsh
git checkout main; git pull
git checkout -b release/v0.12          # = your current minor
git push -u origin release/v0.12
```

This freezes the existing line. Any further `0.12.x` patches go here.

### 2. Bump main's `version.json`

On main:

```jsonc
{
    "$schema": "...",
    "version": "0.13-dev",
    "nuGetPackageVersion": { "semVer": 2.0 },
    "publicReleaseRefSpec": [
        "^refs/heads/release/v\\d+\\.\\d+$"
    ],
    "cloudBuild": { "buildNumber": { "enabled": true } },
    "release": {
        "branchName": "release/v{version}",
        "firstUnstableTag": "dev"
    }
}
```

### 3. Add `AppChannel` to `src/Directory.Build.props`

```xml
<PropertyGroup>
    <AppChannel Condition="'$(AppChannel)' == ''">Dev</AppChannel>
    <DefineConstants Condition="'$(AppChannel)' == 'Dev'">$(DefineConstants);APP_CHANNEL_DEV</DefineConstants>
</PropertyGroup>
```

### 4. Split `ApplicationId` / `ApplicationTitle` per channel in the head csproj

Replace the existing single line with two conditional groups (your app's reverse-DNS prefix instead of `dev.mzikmund`):

```xml
<PropertyGroup Condition="'$(AppChannel)' == 'Prod'">
    <ApplicationTitle>Fluent Stopwatch</ApplicationTitle>
    <ApplicationId>dev.mzikmund.stopwatch</ApplicationId>
</PropertyGroup>

<PropertyGroup Condition="'$(AppChannel)' == 'Dev'">
    <ApplicationTitle>Fluent Stopwatch Dev</ApplicationTitle>
    <ApplicationId>dev.mzikmund.stopwatch.dev</ApplicationId>
</PropertyGroup>
```

### 5. Add a Dev icon variant

Copy `Assets/Icons/icon_foreground.svg` → `icon_foreground_dev.svg` and give it a distinct treatment (e.g. recolor to a caution amber or add a DEV ribbon). Add a single channel-conditional line — `<UnoIconForegroundFile Condition="'$(AppChannel)' == 'Dev'">Assets/Icons/icon_foreground_dev.svg</UnoIconForegroundFile>` — see the template's `AppTemplate.csproj`.

> **Do not** override `UnoIconBackgroundFile` per channel. Uno maps the generated app-icon resource name to the background file's base name, so a per-channel background (e.g. `icon_dev.svg`) renames the resource to `@mipmap/icon_dev` while the manifest still references `@mipmap/icon`, breaking the Android build (`APT2260: resource mipmap/icon not found`). Keep the background constant and vary only the foreground.

### 6. Add `AppEnvironment` + DEV banner

Copy `src/AppTemplate/Infrastructure/AppEnvironment.cs` and `src/AppTemplate/Controls/DevChannelBadge.xaml`(+.cs) from the template. Host the badge in your shell page.

### 7. Update workflows

Copy the template's workflow set wholesale rather than patching your own — the pieces are
interdependent. See [release-pipeline.md](./release-pipeline.md) for what each file does.

- `.github/actions/{setup-dotnet-build,app-version,publish-once}/` — the shared composites.
- `.github/workflows/_build-*.yml` — one reusable workflow per head. These are the only
  place a head is compiled, and they never publish.
- `.github/workflows/build-main.yml` — packages all five heads on `main`, publishes nothing.
- `.github/workflows/release.yml` — builds and publishes on `release/v**`.
- `.github/workflows/{prepare-release,forward-merge,store-ops,store-health}.yml`.
- `.github/scripts/{play_track_op,asc_op}.py`.

Then create the GitHub Environments listed in
[release-pipeline.md](./release-pipeline.md#environments) and set the repo variables
(`ANDROID_PACKAGE_NAME`, `MS_STORE_PRODUCT_ID`, the three `WINDOWS_STORE_*` identity
values, …). The `AppChannel` split is now derived from the branch inside
`.github/actions/app-version`, so there is no per-workflow "Compute app channel" step to
replicate any more.

### 8. Add new secrets to the repo

Windows signing needs **no** secret: Prod packages ship unsigned (the Store re-signs) and
Dev packages are signed with a certificate generated in the job. Both
`BASE64_ENCODED_WINDOWS_PFX` and `BASE64_ENCODED_WINDOWS_PFX_DEV` are retired.

The full secret list, and what happens when each is missing, is in
[release-pipeline.md](./release-pipeline.md#secrets-and-variables).

### 9. Store-side prerequisites

- **Play Console:** confirm your service account has "Release manager" role and the app is currently uploaded to Production at least once (the API can't promote to a track that has no manual baseline).
- **App Store Connect:** confirm an API key with "App Manager" role exists.

### 10. Verify

Push to main → confirm Dev artifacts produced, no publish triggered.
Push to `release/v0.12` (or whatever minor you cut in step 1) → confirm Prod artifacts produced, Play **internal** track updated, TestFlight populated, a Microsoft Store **draft** staged, and tag `v0.12.{patch}` created. Play production, App Store review and the Store commit each wait for an approval in the run's *Review deployments* prompt — see [release-pipeline.md](./release-pipeline.md).

### 11. Clean up

- Remove old "main is a public-release branch" comments from your workflow files.
- Drop any one-off scripts that assumed main published to the Store.

That's it — your app is on the new model.
