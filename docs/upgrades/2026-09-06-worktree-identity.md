---
title: Give each git worktree its own package identity
impact: optional
features: [worktreeIdentity]
platforms: []
---

## Why

Two worktrees of one app otherwise install over each other. With this, each gets its own package identity and
window title, so they run side by side. It does nothing in the main checkout, in CI or on the Prod channel.

## Who is affected

Apps developed in more than one worktree at once. Skip this if `src/WorktreeIdentity.props` already exists, or if you
only ever use one checkout.

## Steps

Follow "Optional: per-worktree identity" in the
[versioning migration guide](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/versioning-migration.md#optional-per-worktree-identity).

Mobile display names now live in `src/MobileLabels.targets` rather than `WorktreeIdentity.targets`. Copy both and
import both from `src/Directory.Build.targets`.

## Verify

Build from a linked worktree and read the id. It should end in `.wt` followed by the worktree's tag:

```bash
dotnet msbuild src/AppTemplate/AppTemplate.csproj -f net10.0-desktop -getProperty:ApplicationId
```
