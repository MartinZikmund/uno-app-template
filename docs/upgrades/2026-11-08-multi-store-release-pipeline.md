---
title: Replace the packaging workflows with the multi-store release pipeline
impact: recommended
features: [packaging]
platforms: []
---

## Why

The old `package-*.yml`, `tag-release.yml` and `static-web-apps-deploy.yml` are gone. In their place, one reusable
workflow per head builds everything, and `release.yml` publishes from `release/v*` branches to Google Play, TestFlight
and the App Store, the Microsoft Store, and your web host. A store only receives a test build automatically:
anything the public would see, such as a Play production rollout, App Store review or a Store commit, waits for an
approval. Store versions are also injected explicitly now, so a build can't silently ship `versionCode=1`.

Full map: [release-pipeline.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/release-pipeline.md).
Day-to-day operations: [release-runbook.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/release-runbook.md).

The web host started as Azure Static Web Apps and is now Cloudflare Workers. If you ship WebAssembly, you're copying
workflows that already deploy there, so follow
[2026-11-09-cloudflare-workers-hosting](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/upgrades/2026-11-09-cloudflare-workers-hosting.md)
as well. It covers the Worker config, the secrets and retiring Azure.

## Who is affected

Apps with the template's release workflows. Skip this if `.github/workflows/release.yml` already exists, or if your app
doesn't release through GitHub Actions.

If you've customized the old packaging workflows, note what you changed before you start, because the new ones replace
them rather than extend them.

## Steps

`mzuno-app upgrade --patch` delivers steps 1 to 4 for the heads your app has, except where you've changed those files.
**Steps 5 to 7 are yours: no patch can create secrets, environments or store records.**

1. **Replace the workflows.** The pieces depend on each other, so copy the set rather than patching your own. Take these
   from [`.github/`](https://github.com/MartinZikmund/uno-app-template/tree/main/.github):
   - `actions/setup-dotnet-build`, `actions/app-version` and `actions/publish-once`, plus
     `actions/prepare-wasm-assets` if you ship WebAssembly
   - `workflows/_build-*.yml`, one per head you ship
   - `workflows/build-main.yml`, `release.yml`, `prepare-release.yml`, `forward-merge.yml`, `store-ops.yml`,
     `store-health.yml`, and `wasm-pr-preview.yml` if you ship WebAssembly
   - `scripts/play_track_op.py` for Android and `scripts/asc_op.py` for iOS
   - the updated `workflows/ci.yml` and `dependabot.yml`

   In a copied file, replace `AppTemplate` with your app's name. Remove the jobs for heads you don't ship from
   `build-main.yml`, `release.yml`, `store-ops.yml` and `store-health.yml`, including their entries under each `needs:`.

2. **Delete the old ones from `.github/`:** `workflows/package-android.yml`, `package-ios.yml`, `package-windows.yml`,
   `static-web-apps-deploy.yml`, `tag-release.yml`, `steps/install_dependencies/` and `Install-WindowsSdkISO.ps1`.

3. **Give the version a prerelease tag.** In `version.json`, change for example `"version": "0.13"` to `"0.13-dev"`.
   Without it `nbgv prepare-release` has nothing to cut and the workflow refuses to run. Add `nbgv` to
   `.config/dotnet-tools.json`, and pin the SDK in `global.json`:

   ```json
   "sdk": { "version": "10.0.100", "rollForward": "latestFeature", "allowPrerelease": false }
   ```

4. **Update the shared build files.** Take the version guards from
   [`src/Directory.Build.props`](https://github.com/MartinZikmund/uno-app-template/blob/main/src/Directory.Build.props) (the
   `_InjectedApplicationVersion` group) and
   [`src/Directory.Build.targets`](https://github.com/MartinZikmund/uno-app-template/blob/main/src/Directory.Build.targets)
   (`SetNbgvVersionForUnoWindows`, `StampWindowsStoreIdentity` and `AssertStoreVersionStamped`). In the props file, also
   take the `NBGV_SetMauiVersionProperties` group.

5. **Create the GitHub Environments.** `release.yml` expects `play-internal`, `testflight`, `ms-store-draft` and
   `web-production` with a `release/v*` branch policy. `play-production`, `appstore-production` and `ms-store-production`
   also need a required reviewer. `store-ops` needs no reviewer and no branch policy, and its secrets must be separate
   copies, because environment secrets don't inherit.

6. **Add secrets and variables** for the stores you use, using the
   [full list](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/release-pipeline.md#secrets-and-variables).
   A missing set skips that store with a warning rather than failing, so you can add them one store at a time.
   `BASE64_ENCODED_WINDOWS_PFX` and `BASE64_ENCODED_WINDOWS_PFX_DEV` are retired and can be deleted.

   If your app is already live on Play, set `VERSION_CODE_OFFSET` so new builds land above your current version code.

7. **Check the store side.**
   - **Play:** the service account needs the Release manager role, and the app must have reached production once by hand.
   - **App Store Connect:** you need an API key with the App Manager role.
   - **Microsoft Store:** the credential must be a Microsoft Entra ID application, and the three `WINDOWS_STORE_*` variables
     must match Partner Center exactly. See
     [windows-packaging.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/windows-packaging.md).

## Verify

1. Push to `main`. `build-main.yml` produces Dev artifacts for your heads and publishes nothing.
2. Run **Cut release branch** (`prepare-release.yml`), then watch `release.yml` on the new branch. Each configured store
   receives its test build, a `v{version}` tag and a draft GitHub Release appear, and the gated jobs wait for approval.
3. Run **Store health** (`store-health.yml`) by hand. It should finish with no issue opened.
