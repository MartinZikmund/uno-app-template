---
description: Every template change must leave existing apps a way to follow along, and optional features must stay optional
---

# Changing the template

This repository generates apps with `dotnet new mzuno-app`, and those apps live on after they're created. Whenever
you change something, ask two questions.

## 1. Can an existing app follow this change?

If the change touches anything an app receives, **add an upgrade note in the same pull request**:

1. Copy `docs/upgrades/_TEMPLATE.md` to `docs/upgrades/YYYY-MM-DD-short-slug.md`, using today's date.
2. Fill in the front matter and the four sections: Why, Who is affected, Steps, Verify.
3. Use feature ids from `.template.config/wizard.json` in `features`, and `android`, `ios`, `windows`, `desktop`
   or `wasm` in `platforms`, so apps without that feature never see the note. Leave a list empty if it concerns every app.

Write for someone whose app has drifted from the template and who has none of your context:

- Give exact steps and real before/after snippets, not "see the PR".
- Keep links absolute, because the note is read outside this repository.
- Call out anything a patch can't deliver, such as secrets, store settings or certificates.
- Never rename or delete a merged note. Apps record notes by file name. If a note was wrong, fix it in place.

Needed for new features, fixes, dependency bumps an app must act on, build and workflow changes, and renames.

**CI enforces this** (`template-upgrade-note.yml`). Everything outside `docs/`, `tools/`, `scripts/`, template-only
tests and `.template.config/` (except `template.json`, which decides what apps get) counts as reaching apps, and only a
newly added note counts. If an app truly has nothing to do, such as after a comment change, say why on its own line
in the PR description:

```text
Upgrade-Note: none - only reorders a comment
```

## 2. Should an app be able to opt out?

If you're adding something an app could reasonably not want, make it a feature rather than a new fixed part of
every app. Follow "Adding a feature" in [`docs/template-authoring.md`](../../docs/template-authoring.md). In short:

- Add a `symbols` entry in `.template.config/template.json`, and its CLI name in `dotnetcli.host.json`.
- List it in `.template.config/wizard.json`, in `ide.host.json` and in `template/overlay/.template-state.json`.
- Exclude its files under a `modifiers` condition, and wrap shared-file changes in markers.
- Add rows to `FeatureOff_RemovesItsTraces` and `FeatureOff_Builds` in `tests/Template.GenerationTests`.

## Markers

The repo must keep compiling with everything on, so markers go in comments. **A real `#if DEBUG` must stay a real
directive**, which is why C# only recognises the `//#if` form.

| File type | Marker |
|---|---|
| `.cs` | `//#if (rating)` … `//#endif` |
| `.xaml` | `<!--  #if (rating)  -->` … `<!--  #endif  -->` (the padded form XAML Styler produces) |
| `.csproj`, `.props`, `.targets`, `.resw`, `.slnx`, `.xml`, `.plist`, `.md` | `<!--#if (rating)-->` … `<!--#endif-->` |
| `.json` | `//#if (rating)` … `//#endif` |
| `.yml` | `#if (rating)` … `#endif` |

Rules that keep generation honest:

- **There's no `#else` for code.** The engine can't uncomment a branch, so shape the code to work without one, such
  as an optional prefix on an expression.
- **`<!--#if (false)-->` hides text that's only about the template** from generated docs.
- **No comment line inside a Markdown table.** It ends the table.
- **Don't write `AppTemplate` in text that names the template's own packages.** `sourceName` rewrites it everywhere,
  which is why they're called `MZikmund.UnoApp.*`.

## Tests

Run `dotnet test tests/Template.GenerationTests/Template.GenerationTests.csproj --filter "TestCategory!=Build"` for
any change to markers or template config. Those tests catch leaked identity, leftover markers and dead doc links.
CI also compiles generated apps.
