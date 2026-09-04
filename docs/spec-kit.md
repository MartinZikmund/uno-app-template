# Spec-driven development with Spec Kit

This repository uses [Spec Kit](https://github.com/github/spec-kit)
([speckit.org](https://speckit.org)), GitHub's spec-driven development toolkit.
Instead of jumping straight to code, you describe a feature, let the workflow
turn it into a reviewed spec, plan, and task list, and then implement against
that plan.

The scaffolding lives in the `.specify/` directory and is committed to the repo,
so the workflow is usable without re-running the Spec Kit CLI. It targets
**Spec Kit 1.0.4**; `.specify/init-options.json` records the version and the
exact options it was generated with:

```text
.specify/
├── .gitignore                   # Machine-local state, deliberately not shared
├── init-options.json            # CLI version + options this tree was generated with
├── memory/
│   └── constitution.md          # Project constitution (principles + governance)
├── templates/
│   ├── constitution-template.md # Source template for the constitution
│   ├── spec-template.md         # Feature specification template
│   ├── plan-template.md         # Implementation plan template
│   ├── tasks-template.md        # Task list template
│   └── checklist-template.md    # Custom checklist template
├── scripts/powershell/          # Prerequisite / setup helper scripts
├── extensions/git/              # Optional Git automation extension
└── integrations/ + workflows/   # Spec Kit ↔ Claude wiring
```

The [constitution](../.specify/memory/constitution.md) is the project's
non-negotiable rulebook (modern C#, MVVM with CommunityToolkit.Mvvm, testable
services, MSTest test-first discipline, Fluent Design, and localization rules).
Every plan validates its `Constitution Check` gate against it.

## The `/speckit-*` workflow

Run these commands (as slash commands in Claude Code) in order for a new
feature. Each step writes artifacts under `specs/<feature>/` and feeds the next
step.

| Command | Purpose |
| --- | --- |
| `/speckit-constitution` | Create or amend the project constitution and keep dependent templates in sync. Run once up front, then only when principles change. |
| `/speckit-specify` | Turn a natural-language feature description into `spec.md` (user stories, requirements, success criteria). |
| `/speckit-clarify` | Ask up to 5 targeted questions to resolve underspecified areas, encoding answers back into the spec. |
| `/speckit-plan` | Produce the implementation plan and design artifacts (`plan.md`, `research.md`, `data-model.md`, etc.); validates the Constitution Check gate. |
| `/speckit-tasks` | Generate a dependency-ordered `tasks.md` grouped by user story. |
| `/speckit-analyze` | Non-destructive consistency/quality check across `spec.md`, `plan.md`, and `tasks.md`. |
| `/speckit-checklist` | Generate a custom review checklist for the feature. |
| `/speckit-implement` | Execute the tasks in `tasks.md`. |
| `/speckit-converge` | Assess the codebase against the spec, plan, and tasks, then append any remaining unbuilt work to `tasks.md` so `/speckit-implement` can finish it. |
| `/speckit-taskstoissues` | Convert tasks into GitHub issues (optional). |

Optional Git automation (under `.specify/extensions/git/`) provides
`/speckit-git-feature`, `/speckit-git-commit`, `/speckit-git-initialize`,
`/speckit-git-remote`, and `/speckit-git-validate` for branch creation, commits,
and branch-name validation.

A typical flow:

```text
/speckit-specify  Add an onboarding flow that ...
/speckit-clarify
/speckit-plan
/speckit-tasks
/speckit-implement
```

## Conventions to keep in mind

- Implementation MUST follow the [constitution](../.specify/memory/constitution.md).
  When the UI is involved, apply the Fluent Design and localization rules; when
  adding logic, put it in testable `AppTemplate.Core` services with MSTest
  coverage.
- Run `dotnet format` before staging and build the affected project(s) before
  calling a change done.
- The `.specify/templates/*.md` files are intentionally full of `[PLACEHOLDER]`
  tokens — they are filled in per feature by the commands above. Do not "fix"
  the placeholders in the templates themselves.

## Upgrading Spec Kit

Re-run the CLI with the options already recorded in `.specify/init-options.json`,
pinning the release you want:

```bash
uvx --from git+https://github.com/github/spec-kit.git@v1.0.4 specify init \
    --here --force --non-interactive --integration claude --script ps --extension git
```

`--force` merges into the existing tree. `.specify/memory/constitution.md` is
project content and is preserved across upgrades — verify that after any bump.
