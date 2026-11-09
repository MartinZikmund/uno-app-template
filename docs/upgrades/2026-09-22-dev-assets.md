---
title: Generate the Dev icon and splash at build time
impact: optional
features: [devAssets]
platforms: []
---

## Why

There's no Dev artwork to draw or keep in sync. Dev builds stamp a badge onto your production icon and splash.

## Who is affected

Apps with hand-made Dev artwork, or with none. Skip this if `src/DevAssets.targets` already exists.

## Steps

1. Copy [`src/DevAssets.targets`](https://github.com/MartinZikmund/uno-app-template/blob/main/src/DevAssets.targets) and
   [`src/DevAssets.Task.cs`](https://github.com/MartinZikmund/uno-app-template/blob/main/src/DevAssets.Task.cs) into your `src/`.
2. Import the targets from `src/Directory.Build.targets`:

   ```xml
   <Import Project="$(MSBuildThisFileDirectory)DevAssets.targets" />
   ```

3. Remove any per-channel `UnoIconForegroundFile` or `UnoIconBackgroundFile` overrides from your head project, and
   delete the Dev artwork they pointed at.

## Verify

Build the Dev channel and look at the generated icon under `obj/`. It has a badge at the bottom centre. A build with
`-p:AppChannel=Prod` has none.
