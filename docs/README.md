# Docs

One page per topic. Keep it that way — see [`.claude/rules/docs.md`](../.claude/rules/docs.md).

## Building & tooling

- [building.md](./building.md) — target frameworks, per-platform prerequisites, build and run commands.
- [xaml-styler.md](./xaml-styler.md) — XAML formatting rules and how CI enforces them.

## Architecture

- [json-aot-serialization.md](./json-aot-serialization.md) — `JsonSerializerContext` conventions for AOT-safe, trim-safe JSON serialization.
- [views.md](./views.md) — `ViewBase<TViewModel>`, `IViewBase`, and how views resolve their view models.

## Release

- [versioning.md](./versioning.md) — the Dev/Prod channel model, git-height versions, side-by-side identity.
- [versioning-migration.md](./versioning-migration.md) — applying that model to an existing app.

## Design history

- [superpowers/](./superpowers/) — specs and implementation plans kept for context on why things are shaped the way they are. Not reference documentation.
