---
name: upgrade-from-template
description: Bring later improvements from the Uno App template into this app. Use when the user asks to upgrade, update or sync the app with the template, asks what changed in the template, or mentions .template-state.json or `mzuno-app upgrade`.
---

# Upgrade this app from the template

`.template-state.json` records the template version and features this app was created with, and which
upgrade notes it already has. Upgrade notes are the source of truth for what changed and why.

## Steps

1. **Work on a clean branch.** Stop and ask if the working tree has uncommitted changes.
2. **Get the plan.** Update the tool, then list what's pending:

   ```bash
   dotnet tool update -g MZikmund.UnoApp.Cli
   mzuno-app upgrade --json
   ```

   Notes for features this app doesn't have are already filtered out. If the tool can't be installed, read
   `docs/upgrades/` in the template repository (`upgradesUrl` in the state file) and filter by each note's
   `features` front matter yourself.
3. **Go through the notes oldest first**, using `mzuno-app upgrade --show <id>`. For each one:
   - Read "Why" and "Who is affected". If it doesn't apply, tell the user why and skip it.
   - Follow "Steps". The app's code has diverged from the template, so apply the intent rather than pasting
     blindly, and keep the app's own changes.
   - Run "Verify".
   - Record it with `mzuno-app upgrade --mark-applied <id>` and commit, one commit per note.
4. **`mzuno-app upgrade --patch` is a shortcut, not the plan.** It three-way merges the template's own changes
   into the working tree, which is good for mechanical changes. Resolve any conflict markers, then still walk
   the notes, because they carry manual steps such as secrets and store settings that a patch can't contain.
5. **Finish with a build and the tests.** Report anything you skipped, and any manual step left for the user.
