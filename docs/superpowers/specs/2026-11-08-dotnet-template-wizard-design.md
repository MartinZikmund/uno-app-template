# A real `dotnet new` template, a wizard, and upgrade notes

**Date:** 2026-11-08

## Intent

The repository was "copy it, rename it, delete what you don't need". The README said a rename script wasn't worth
writing, and that adoption was by hand. Two things hurt: starting an app took an afternoon of careful deletion, and an
app that had started had no way to pick up later template fixes short of reading the commit log.

The goals, in Martin's words: "a proper .NET template with a visual/CLI configurable wizard that allows picking the
features the user wants and does not want", and "with future new features/changes the agents also leave docs that
allow easy migration of existing apps".

## Decisions Martin made

- **Visual wizard:** a local Uno WebAssembly app on Uno.Sdk 7.0, falling back to a desktop app if WASM can't work. It
  follows `/winui-design`.
- **Upgrades:** migration docs, plus a plan command, plus a three-way patch.
- **Feature picker granularity:** everything sensible (platform heads, each service, sample language, build helpers,
  tests, CI, release workflows, agent files, Spec Kit, community files), all on by default.
- **Delivery:** commit in logical steps and open a draft PR. Nothing is published to NuGet; publishing is a workflow
  he triggers.

## Decisions made on his behalf

Each of these is easy to reverse before release.

- **Names.** Packages `MZikmund.UnoApp.Templates` and `MZikmund.UnoApp.Cli`, with `mzuno-app` as both the template
  short name and the tool command. Uno's own template already owns `unoapp`. The ids avoid `AppTemplate`, which
  `sourceName` would rewrite.
- **The repository root stays the template source.** There's no `template/content` folder, so `src/` stays a normal app
  and about twenty open PRs don't all conflict. Only `template/overlay/` holds app-only files.
- **Markers in comments**, so the repo still compiles with everything on.
- **Upgrade notes are dated files tracked by id, not by version.** A version isn't known until merge, and a "last applied
  version" pointer would skip a note that merged out of date order. `.template-state.json` lists applied ids, and a new
  file can't conflict, matching the reasoning in `.claude/rules/docs.md`.
- **CI gate with a reasoned waiver.** `Upgrade-Note: none - <reason>` in the PR description. Docs-only and template-only
  paths are exempt. A rule without a gate would quietly stop being followed.
- **One manifest.** The wizards read `template.json` and `wizard.json` rather than holding their own feature list, and
  tests fail if the two disagree.
- **The server is hardened** because it can write to disk: loopback only, Host header check against DNS rebinding,
  SameSite=Strict session cookie, server-side revalidation, and it refuses non-empty folders.
- **The app's `LICENSE` is not the template's.** Generating an MIT `LICENSE` would imply the app is MIT-licensed. The
  notice moves to `docs/TEMPLATE-LICENSE.txt`, which satisfies MIT's notice requirement.

## How upgrades work

`--patch` generates the app's old template version and the new one with the app's own name and options, commits both
to a temporary repository, diffs them, fetches those objects into the app, and runs `git apply --3way`. Git therefore
has a true common ancestor for each file: the app's edits survive, new files arrive under the app's names, features
the app dropped stay dropped, and real clashes become ordinary conflict markers.

The notes aren't made redundant by this. They carry the why and anything a diff can't, such as secrets, store settings
and certificates. Hand-copied apps enter through `mzuno-app adopt`, which infers features from their files for the owner
to review.

## Non-goals

- Automatically upgrading apps, or opening PRs in them.
- Removing features from an existing app. A patch applies the app's recorded options; it doesn't change them.
- A hosted wizard. The tool runs locally because it creates files locally.
