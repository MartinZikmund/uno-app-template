---
description: Branching, commit, and versioning conventions
---

# Git & releases

## Commits — Conventional Commits
- Format: `type: subject`, where type is `feat | fix | chore | docs | refactor | test | ci | build`.
- A **scope is optional** — add one when it sharpens intent (`feat(navigation): ...`), omit it otherwise (`fix: ...`).
- Imperative mood, concise, no trailing period.

## Branches
- Work on `feature/<short-name>` branches off `main`; open a PR back to `main`.
- Releases happen on `release/v{major}.{minor}` branches.

## Versioning
- Versions are computed automatically by **Nerdbank.GitVersioning** from `version.json` and git height. The git height is the **patch** component, so `main` produces `X.Y.{height}` (prerelease-tagged) and a `release/v{x.y}` branch produces stable `X.Y.{height}`. The height does **not** reset when a release branch is cut, so patch numbers are monotonic but not contiguous.
- `version.json`'s `version` must carry a prerelease tag (`"0.2-dev"`, not `"0.2"`), or `nbgv prepare-release` has nothing to cut. See [`docs/versioning.md`](../../docs/versioning.md).
- **Don't hand-edit version numbers** in manifests — the `SetNbgvVersionForUnoWindows` target injects them at build time.
