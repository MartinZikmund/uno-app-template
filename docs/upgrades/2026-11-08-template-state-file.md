---
title: Record the app's template state so upgrades can be tracked
impact: recommended
features: []
platforms: []
---

## Why

The template is now a real `dotnet new` template with upgrade notes like this one. `.template-state.json` remembers
which features an app has and which notes it has applied, so `mzuno-app upgrade` can show only what's relevant.

## Who is affected

Apps copied from the repository by hand before this note. Apps created with `dotnet new mzuno-app` or the wizard
already have the file, and this note is already marked as applied for them.

## Steps

1. Install the tool:

   ```bash
   dotnet tool install -g MZikmund.UnoApp.Cli
   ```

2. From the app's repository root, create the state file. The tool infers the platforms and features from your files:

   ```bash
   mzuno-app adopt --name YourApp
   ```

   Pass `--title`, `--app-id` and `--publisher` too if you plan to use `mzuno-app upgrade --patch`.

3. Review `.template-state.json` and correct anything the inference got wrong.
4. If the app already has some notes' changes, mark them so they stop being listed:

   ```bash
   mzuno-app upgrade --mark-applied 2026-05-28-versioning-model
   ```

5. Commit the file.

## Verify

`mzuno-app upgrade` lists only notes you still want to apply.
