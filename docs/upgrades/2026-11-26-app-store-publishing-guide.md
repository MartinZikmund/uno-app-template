---
title: App Store publishing guide
impact: optional
features: [packaging]
platforms: [ios]
---

## Why

There was no record of what publishing an app to the App Store takes beyond the CI workflows. The new
[app-store-publishing.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/app-store-publishing.md)
walks through the whole first publish, with the decisions and caveats found while publishing Merry Countdown: the
certificate and key handling, the toolchain traps that produce an outdated-SDK build, the Kids category's permanent
choices, the in-app purchase's irreversible settings, and what App Review needs to see.

## Who is affected

Apps with the iOS head and packaging that haven't been published on the App Store yet. Skip it if your app is
already live there.

## Steps

1. Copy `docs/app-store-publishing.md` from the template into your app's `docs/`, and add its line to `docs/README.md`
   under Release.
2. Apply
   [2026-11-26-apple-privacy-manifest-and-xcode-root](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/upgrades/2026-11-26-apple-privacy-manifest-and-xcode-root.md)
   if you haven't yet.

Everything else in the guide is App Store Connect and Developer portal work that a patch can't deliver.

## Verify

The guide is in `docs/` and linked from `docs/README.md`.
