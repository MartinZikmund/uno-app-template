---
title: Split mobile labels out of worktree identity and fit long app titles
impact: recommended
features: []
platforms: []
---

## Why

Mobile display names were built inside `WorktreeIdentity.targets`, so removing worktree identity also lost the "Dev"
suffix on Android and iOS. Separately, the worktree tag was capped assuming a 16-character app title, so a longer
title could push the Windows `ShortName` past its 40-character limit and fail packaging.

## Who is affected

Apps with `src/WorktreeIdentity.targets`. Skip this otherwise.

## Steps

1. Copy [`src/MobileLabels.targets`](https://github.com/MartinZikmund/uno-app-template/blob/main/src/MobileLabels.targets)
   into your `src/`. If you changed `IosShortNameBase`, the head project's `MSBuildProjectName` conditions or the label
   logic, carry those changes across.
2. In `src/WorktreeIdentity.targets`, delete everything from the `Mobile display names.` comment up to the
   `WebAssembly: surface the derived per-worktree dev-server port` comment.
3. Import the new file from `src/Directory.Build.targets`, after `WorktreeIdentity.targets`:

   ```xml
   <Import Project="$(MSBuildThisFileDirectory)MobileLabels.targets" />
   ```

4. In `src/WorktreeIdentity.targets`, replace the `<ApplicationTitle>` line in the identity suffix group:

   ```xml
   <_WtTitleRoom>$([MSBuild]::Subtract(37, $(ApplicationTitle.Length)))</_WtTitleRoom>
   <_WtTitleTag Condition="$(_WtTitleRoom) &gt;= $(_WtLongTag.Length)">$(_WtLongTag)</_WtTitleTag>
   <_WtTitleTag Condition="'$(_WtTitleTag)' == '' and $(_WtTitleRoom) &gt; 0">$(_WtLongTag.Substring(0, $(_WtTitleRoom)))</_WtTitleTag>
   <ApplicationTitle Condition="'$(_WtTitleTag)' != ''">$(ApplicationTitle) ($(_WtTitleTag))</ApplicationTitle>
   ```

## Verify

Build the Android head on the Dev channel. The launcher label still ends in `Dev`. From a linked worktree,
`-getProperty:ApplicationTitle` returns at most 40 characters.
