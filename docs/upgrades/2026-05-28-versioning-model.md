---
title: Adopt Dev and Prod channels with git-height versions
impact: recommended
features: []
platforms: []
---

## Why

Dev builds install next to the released app, and only `release/v*` branches publish to stores. A push to `main`
can no longer reach users by accident.

## Who is affected

Apps copied from the template before May 2026. Skip this if `src/Directory.Build.props` already sets `AppChannel`.

## Steps

Follow the
[versioning migration guide](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/versioning-migration.md)
from "Pre-flight" through "Clean up". Steps 8 and 9 cover repository secrets and store settings, which only you can
change.

## Verify

Push to `main` and confirm Dev artifacts are produced with nothing published. Push to your `release/v*` branch and
confirm Prod artifacts are produced and a `v{version}` tag is created.
