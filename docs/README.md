# Docs

One page per topic. Keep it that way.

## Building & tooling

- [building.md](./building.md) — target frameworks, per-platform prerequisites, build and run commands.
- [community-toolkit-copy.md](./community-toolkit-copy.md) — the temporary source copy of the CommunityToolkit pieces the app uses, until the toolkit supports Uno 7.
<!--#if (devAssets)-->
- [dev-assets.md](./dev-assets.md) — the Dev-channel icon and splash, badged at build time from your prod artwork.
<!--#endif-->
<!--#if (specKit)-->
- [spec-kit.md](./spec-kit.md) — the Spec Kit spec-driven development workflow (`/speckit-*` commands and the project constitution).
<!--#endif-->
- [template-upgrades.md](./template-upgrades.md) — bringing later template improvements into an app created from it.
<!--#if (hasTvos)-->
- [tvos.md](./tvos.md) — the Apple TV head: icons, platform differences, CI, and how to drop it.
<!--#endif-->
<!--#if (worktreeIdentity)-->
- [worktree-identity.md](./worktree-identity.md) — per-worktree package identity, so two git worktrees can be installed and run side by side.
<!--#endif-->
<!--#if (xamlStyler)-->
- [xaml-styler.md](./xaml-styler.md) — XAML formatting rules and how CI enforces them.
<!--#endif-->

## Architecture

- [json-aot-serialization.md](./json-aot-serialization.md) — `JsonSerializerContext` conventions for AOT-safe, trim-safe JSON serialization.
- [views.md](./views.md) — `ViewBase<TViewModel>`, `IViewBase`, and how views resolve their view models.

## Release

- [deployment.md](./deployment.md) — hosting the WebAssembly head on Cloudflare Workers: production, PR previews, one-time setup.
- [release-pipeline.md](./release-pipeline.md) — what each workflow builds and publishes, environments, secrets, fork behaviour.
- [release-runbook.md](./release-runbook.md) — cutting a release, shipping a patch, halting a bad rollout.
- [versioning.md](./versioning.md) — the Dev/Prod channel model, git-height versions, side-by-side identity.
<!--#if (false)-->
- [versioning-migration.md](./versioning-migration.md) — applying that model to an existing app.
<!--#endif-->
<!--#if (hasWindows)-->
- [windows-packaging.md](./windows-packaging.md) — the MSIX path, Store identity, and Microsoft Store submission.
<!--#endif-->

<!--#if (false)-->
## Working on the template

- [template-authoring.md](./template-authoring.md) — how the template is built, how to add a feature, and how to test it.
- [template-wizard.md](./template-wizard.md) — using and building the `mzuno-app` wizard.
- [upgrades/](./upgrades/README.md) — one note per template change, so existing apps can follow along.

## Design history

- [superpowers/](./superpowers/) — specs and implementation plans kept for context on why things are shaped the way they are. Not reference documentation.
<!--#endif-->
