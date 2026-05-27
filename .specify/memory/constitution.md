<!--
SYNC IMPACT REPORT
==================
Version change: TEMPLATE (unfilled) → 1.0.0
Bump rationale: Initial ratification of the project constitution, establishing
concrete principles for this Uno Platform (WinUI) C# application.

Modified principles (template slot → ratified principle):
  - [PRINCIPLE_1_NAME] → I. Modern, Consistent C#
  - [PRINCIPLE_2_NAME] → II. MVVM with CommunityToolkit.Mvvm
  - [PRINCIPLE_3_NAME] → III. Testable Services (Logic Out of the UI)
  - [PRINCIPLE_4_NAME] → IV. Test-First Discipline (NON-NEGOTIABLE)
  - [PRINCIPLE_5_NAME] → V. Fluent Design & First-Class Localization

Added sections:
  - Technology Stack & Constraints (was [SECTION_2_NAME])
  - Development Workflow & Quality Gates (was [SECTION_3_NAME])
  - Governance

Removed sections: none.

Templates requiring updates:
  - ✅ .specify/templates/plan-template.md — Constitution Check gate is generic
    ("[Gates determined based on constitution file]"); no change required.
  - ✅ .specify/templates/spec-template.md — mandatory sections still align; no change required.
  - ✅ .specify/templates/tasks-template.md — task categories (tests, models,
    services) align with Principles III & IV; no change required.

Follow-up TODOs: none. RATIFICATION_DATE set to the date of this initial adoption.
-->

# AppTemplate Constitution

## Core Principles

### I. Modern, Consistent C#

The codebase MUST use modern, idiomatic C# consistently:

- File-scoped namespaces everywhere; nullable reference types enabled
  (`<Nullable>enable</Nullable>`).
- Target-typed `new()` with an explicit type on the left
  (`MyService service = new();`); use `var` only when the type is not on the
  left side (`var items = collection.Where(...).ToList();`).
- Always use curly braces for single-line `if`/`for`/`foreach`/`while`.
- Prefer expression-bodied members for single-line methods and properties.
- `dotnet format` MUST be run before changes are staged.

**Rationale**: A single, enforced style keeps a multi-target Uno codebase
readable and reviewable, and removes formatting churn from diffs.

### II. MVVM with CommunityToolkit.Mvvm

The presentation layer MUST follow MVVM using CommunityToolkit.Mvvm:

- ViewModels derive from `ObservableObject` and use `[ObservableProperty]` and
  `[RelayCommand]` rather than hand-written boilerplate.
- One ViewModel per page/view, named consistently (`MainPage` → `MainViewModel`).
- ViewModels stay thin: they wire services to the UI and hold UI state only.
  Business logic does NOT live in ViewModels or code-behind.
- Dependencies are provided by constructor injection via DI — no service locator.

**Rationale**: Thin, convention-named ViewModels backed by DI keep views
declarative and make behavior predictable across the codebase.

### III. Testable Services (Logic Out of the UI)

Business logic MUST live in services, not in the UI:

- Extract logic from ViewModels and code-behind into services in
  `AppTemplate.Core` (Services/Navigation/Infrastructure).
- Services are registered with DI and consumed through interfaces where it aids
  testing or platform abstraction.
- `AppTemplate.Core` MUST NOT take a hard dependency on view-layer types; it
  remains independently buildable and testable.

**Rationale**: Keeping logic in platform-agnostic services is what makes the app
unit-testable without spinning up the UI, and keeps cross-platform code shared.

### IV. Test-First Discipline (NON-NEGOTIABLE)

Tests are written first or alongside implementation, never bolted on afterward:

- Test framework is MSTest (via `MSTest.Sdk`), running under Microsoft Testing
  Platform; tests live in `AppTemplate.Core.Tests`.
- Test names follow `MethodName_Scenario_ExpectedResult`
  (e.g. `CalculateTotal_WithDiscount_ReturnsReducedPrice`).
- New or changed service logic MUST be covered by tests; bug fixes MUST add a
  test that reproduces the defect before the fix.

**Rationale**: TDD on the service layer locks in behavior, documents intent, and
prevents regressions in shared, cross-platform code.

### V. Fluent Design & First-Class Localization

The UI MUST follow Fluent Design and be localizable from the start:

- Rely on WinUI built-in styles and theme resources (`StaticResource`,
  `ThemeResource`); do not hardcode colors, sizes, or fonts that have a theme
  resource equivalent.
- XAML includes structural comments delineating sections (e.g. `<!-- Header -->`,
  `<!-- Content -->`, `<!-- Actions -->`).
- Localization uses the markup localization extension with short, descriptive
  keys (`{x:Bind loc:Resources.WelcomeTitle}`); `x:Uid` MUST NOT be used. Keys
  are clear words (`WelcomeTitle`, `SaveButton`, `ErrorNotFound`) — never GUIDs
  or numeric IDs.

**Rationale**: Themed resources keep the app correct in light/dark and high
contrast, and key-based localization makes every string translatable by default.

## Technology Stack & Constraints

- **Platform**: Uno Platform (WinUI) targeting .NET 10; pinned via `global.json`
  (`Uno.Sdk`, `MSTest.Sdk`). Prerelease SDKs are disallowed unless `global.json`
  says otherwise.
- **Solution layout** MUST follow the established structure:
  - `src/AppTemplate/` — heads/UI: `Views/`, `ViewModels/`, `Converters/`,
    `Markup/`, `Resources/`, `Strings/`, `Assets/`, `Platforms/`, `Infrastructure/`.
  - `src/AppTemplate.Core/` — shared, testable logic: `Services/`, `ViewModels/`,
    `Navigation/`, `Infrastructure/`.
  - `tests/AppTemplate.Core.Tests/` — MSTest unit tests.
- **Cross-platform safety**: code in `AppTemplate.Core` MUST remain free of
  head-specific or platform-specific APIs so all targets keep building.
- **MVVM toolkit**: CommunityToolkit.Mvvm is the only sanctioned MVVM primitive
  source for ViewModels and commands.

## Development Workflow & Quality Gates

- **Format gate**: `dotnet format` MUST pass (no diffs) before staging.
- **Build gate**: when C# changes, the affected project(s) MUST build
  (`dotnet build`) before the change is considered done.
- **Test gate**: service/logic changes MUST be accompanied by passing MSTest
  tests; the test project MUST be green.
- **Spec-driven changes**: non-trivial features SHOULD flow through the Spec Kit
  workflow (`/speckit-specify` → `/speckit-plan` → `/speckit-tasks` →
  `/speckit-implement`), with the `Constitution Check` in the plan validated
  against this document.
- **Reviews**: pull requests MUST verify compliance with the principles above;
  any deviation MUST be justified in the PR (and recorded in the plan's
  Complexity Tracking when it stems from a constitution gate).

## Governance

This constitution supersedes other conventions where they conflict. It pairs
with the repository's `CLAUDE.md` / agent guidance, which provides the same
rules in runtime-prompt form; the two MUST be kept consistent.

- **Amendments**: proposed via pull request that edits this file, states the
  rationale, and notes any dependent template/doc updates. Amendments take
  effect when merged.
- **Versioning** (semantic): MAJOR for backward-incompatible governance or
  principle removals/redefinitions; MINOR for a new principle/section or
  materially expanded guidance; PATCH for clarifications and wording fixes.
- **Compliance review**: every PR and code review verifies adherence; complexity
  or rule deviations MUST be explicitly justified, never silently introduced.
- **Runtime guidance**: agents and contributors use `CLAUDE.md` and the
  `.specify/` templates as the operational expression of these principles.

**Version**: 1.0.0 | **Ratified**: 2026-05-27 | **Last Amended**: 2026-05-27
