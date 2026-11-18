---
title: Pick the Xcode the iOS and tvOS workloads need
impact: recommended
features: []
platforms: [ios, tvos]
---

## Why

`dotnet workload restore` installs the newest iOS and tvOS workloads, and each one requires a specific Xcode version.
The `xcode-27` runner image defaults to Xcode 27.1, so once the workloads moved to SDK 27.2 the iOS and tvOS builds
failed with:

```text
error : This version of .NET for tvOS (27.2.10000) requires Xcode 27.2. The current version of Xcode is 27.1.
```

A new composite action reads the required version from the installed SDK pack and selects that Xcode, so the next
workload bump doesn't break the build the same way.

## Who is affected

Apps with the iOS or tvOS head that build them with `.github/workflows/_build-ios.yml` or `_build-tvos.yml`.

## Steps

1. Copy [`.github/actions/select-xcode/action.yml`](https://github.com/MartinZikmund/uno-app-template/blob/main/.github/actions/select-xcode/action.yml)
   into your app.

2. In `.github/workflows/_build-ios.yml`, replace the Xcode version step that follows `setup-dotnet-build`:

   ```diff
   -      # No Xcode selection step: latest-stable would pick the 27.2 RC, which the SDK rejects.
   -      - name: Show Xcode version
   -        run: xcodebuild -version
   +      - uses: ./.github/actions/select-xcode
   +        with:
   +          platform: iOS
   +          tfm: ${{ env.TFM }}
   ```

3. Do the same in `.github/workflows/_build-tvos.yml`, with `platform: tvOS`.

## Verify

Run the **Package (main)** workflow (`workflow_dispatch`, `only: ios,tvos`). The **Select Xcode** step prints the Xcode
version it picked, and the build steps after it pass. If the image doesn't have the Xcode the workload wants, the
step fails and lists the versions that are installed.
