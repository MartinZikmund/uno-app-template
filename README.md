# App Template

A production-shaped starting point for a cross-platform [Uno Platform](https://platform.uno/) app.
Five platform heads from a single project, plain WinUI/XAML with CommunityToolkit.Mvvm, and the
plumbing you would otherwise rebuild every time: navigation, dependency injection, theming,
localization, dialogs, versioning, and release pipelines.

Copy it, rename it, delete what you don't need.

## What's in the box

| | |
|---|---|
| **Five heads, one project** | Android, iOS, Windows (WinAppSDK), Desktop (Skia), and WebAssembly from `src/AppTemplate`. See [docs/building.md](./docs/building.md). |
| **MVVM with CommunityToolkit.Mvvm** | `ObservableObject`, `[ObservableProperty]` partial properties, and `[RelayCommand]`. View models live in `AppTemplate.Core` so they unit-test without a UI head. See [docs/views.md](./docs/views.md). |
| **Type-driven navigation** | `INavigationService.Navigate<TViewModel>()`, with views registered explicitly rather than by reflection. |
| **DI with the guardrails on** | Scope validation enabled, so a captive dependency fails at startup instead of in production. Per-window scopes for window-bound services. |
| **Services already wired** | Theming, preferences, dialogs and confirmations, app rating, share, launcher, display-request, and app-update checks. |
| **Localization from the start** | `{markup:Localize Key=...}` in XAML, `IStringLocalizer` in code, English and Czech resources included. |
| **Side-by-side Dev builds** | Nerdbank.GitVersioning with Dev and Prod channels that install alongside each other, distinct icons included. See [docs/versioning.md](./docs/versioning.md). |
| **CI that packages** | Build and smoke-test workflows plus Windows, Android, iOS packaging and WebAssembly deployment. XAML formatting is enforced on every PR — see [docs/xaml-styler.md](./docs/xaml-styler.md). |
| **Written for coding agents** | [`AGENTS.md`](./AGENTS.md) and [`.claude/rules/`](./.claude/rules/) carry the conventions an agent needs before it writes a line. |

## Using this template

There is no rename script — the steps below are the whole job, and doing them by hand once is
clearer than debugging a script that half-worked.

1. **Start your repo.** Use this repository as a GitHub template, or clone it and point `origin`
   at your own remote.

2. **Rename `AppTemplate` to your app.** It appears in roughly 59 C# namespace declarations plus:

   ```text
   src/AppTemplate/                          folder + AppTemplate.csproj
   src/AppTemplate.Core/                     folder + AppTemplate.Core.csproj
   tests/AppTemplate.Core.Tests/             folder + .csproj
   src/AppTemplate.slnx
   src/.run/AppTemplate.run.xml
   src/.vscode/launch.json, tasks.json
   src/AppTemplate/Properties/launchSettings.json
   src/AppTemplate/Platforms/WebAssembly/LinkerConfig.xml
   ```

   A find-and-replace of `AppTemplate` → `YourApp` across the repo, then renaming the folders and
   project files, covers all of it.

3. **Claim your identity.** In `src/AppTemplate/AppTemplate.csproj`, set `ApplicationPublisher`,
   and set `ApplicationTitle` and `ApplicationId` for **both** the `Prod` and `Dev` channel
   property groups — they must differ, that's what lets Dev install side by side. Then update the
   display names in `src/AppTemplate/Platforms/Android/Resources/values*/Strings.xml`.

4. **Replace the artwork.** Drop your own SVGs into `src/AppTemplate/Assets/Icons` and
   `src/AppTemplate/Assets/Splash`. Keep `icon_transparent.svg` and `icon.svg` as the background
   filenames unless you also update the `UnoIcon*` properties — the generated Android
   `@mipmap/icon` resource name is derived from them.

5. **Reset the version.** `version.json` starts at `0.1`. Set it to whatever your first release
   should be; git height supplies the rest.

6. **Translate or trim.** Keep both `src/AppTemplate/Strings/en` and `.../cs`, or delete the `cs`
   folder and its Android `values-cs` counterpart if you only ship one language.

7. **Delete what you don't need.** Sample views, the Czech resources, the rating service — none of
   it is load-bearing. Removing a service means deleting its files and its registration in
   `App.RegisterServices`.

### If you're a coding agent

Read [`AGENTS.md`](./AGENTS.md) first — it points at [`.claude/rules/`](./.claude/rules/), which
carries the conventions this repo actually enforces: code style, the Core/head split, testing, git,
and documentation. Adding a feature means adding a page under [`docs/`](./docs/), never appending
prose to this file.

## Quickstart

```bash
dotnet tool restore                                                   # XAML Styler, once per clone
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop    # fastest head, no workloads
```

Other heads, per-platform prerequisites, and how to run the packaged Windows app live in
[docs/building.md](./docs/building.md).

## Docs

[`docs/`](./docs/) holds a page per topic — start at [docs/README.md](./docs/README.md).

A cross-platform Uno Platform / WinUI app template targeting .NET 10.

## JSON serialization (AOT / iOS / NativeAOT)

### Why a per-app `JsonSerializerContext`

Reflection-based JSON serialization (`JsonSerializer.Serialize<T>(value)` with no
extra options) fails at runtime on platforms that disallow runtime code generation,
most notably **iOS NativeAOT** and trimmed **WASM** builds. The .NET trimmer cannot
statically determine which types will be serialized, so the required metadata is
stripped away.

The solution is **source-generated serialization** via
[`JsonSerializerContext`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation).
You annotate a `partial` class with `[JsonSerializable(typeof(MyModel))]` for every
type your app serializes, and the compiler generates all required type metadata at
build time. No runtime reflection is needed.

A `JsonSerializerContext` **cannot be shared as a NuGet package** because it must
contain the closed set of types known at compile time — types that are specific to
each app. Every app therefore needs its own context class.

### `JsonSourceGenerationOptions` conventions

Use two distinct option profiles depending on the use case:

| Use case | `WriteIndented` | Notes |
|---|---|---|
| **Storage / network** (default) | `false` | Compact; smaller payloads, faster I/O. |
| **Export / debug files** | `true` | Human-readable; easier to inspect. |

For both profiles use `CamelCase` property naming and
`WhenWritingNull` to omit null values unless your API contract requires them.

### Pattern: registering types in the context

Declare a `partial` class that inherits `JsonSerializerContext`. Annotate it with
one `[JsonSerializable]` attribute per serializable type, including collection
variants (`List<T>`, `T[]`, etc.) that you pass to `JsonSerializer`.

```csharp
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(MyModel))]
[JsonSerializable(typeof(List<MyModel>))]
[JsonSerializable(typeof(AnotherModel))]
public partial class MyAppJsonContext : JsonSerializerContext
{
}
```

Then use the generated static `Default` instance directly:

```csharp
// Serialize
string json = JsonSerializer.Serialize(model, MyAppJsonContext.Default.MyModel);

// Deserialize
MyModel? obj = JsonSerializer.Deserialize(json, MyAppJsonContext.Default.MyModel);
```

Or compose it into `JsonSerializerOptions` for helpers that accept options:

```csharp
JsonSerializerOptions options = new()
{
    TypeInfoResolver = MyAppJsonContext.Default,
};
```

### Stub file

A ready-to-copy stub lives at
`src/AppTemplate.Core/Models/AppTemplateJsonContext.cs`.
It includes:

- An example `ExampleModel` record showing a minimal serializable type.
- A `[JsonSourceGenerationOptions]` declaration with the recommended defaults.
- `[JsonSerializable]` registrations for the model and its `List<T>` variant.
- Inline comments explaining how to adapt and use it.

Rename the class, swap in your own types, and add a `[JsonSerializable]` entry for
each type you need. Delete `ExampleModel` once you have real models registered.

## Versioning

This template uses Nerdbank.GitVersioning. `main` produces `0.X.0-dev.{height}` prerelease builds with a Dev-channel identity that installs side-by-side with the Store version. Stable releases come from `release/v{minor}` branches. See [docs/versioning.md](./docs/versioning.md) for the full model and [docs/versioning-migration.md](./docs/versioning-migration.md) to apply it to an existing app.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
