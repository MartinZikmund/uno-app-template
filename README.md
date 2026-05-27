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

## One JsonSerializerContext per boundary

Every external API or distinct data-shape boundary in this template owns its own
[`JsonSerializerContext`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonserializercontext).
This is a deliberate convention — not a limitation of the framework.

> **Related:** Issue #12 documents the per-app AOT context for the app's own models.
> The convention here extends that idea to third-party/external data shapes.

### Why one context per boundary?

Source-generated serialization contexts are **zero-cost at runtime** — the
serialization metadata is emitted by the compiler, not reflected at startup.
The cost is paid at **build time** (code generation).

The risk is the opposite of reflection: a single monolithic context that lists
every type in the codebase couples unrelated shapes together, increases
incremental-build churn, and makes it harder to understand which types cross
a given boundary.

Keeping contexts small and scoped to one logical boundary means:

- Startup stays fast — no reflection, and no giant registration table to scan.
- Each context is independently evolvable; renaming a field in the weather API
  response type cannot accidentally break storage serialization.
- AOT / NativeAOT / iOS builds stay compliant: the closed set of types required
  by each context is always explicitly declared.

### Logical boundaries

| Context | Owns |
|---------|------|
| `AppStorageJsonContext` | Models persisted to local app storage |
| `WeatherApiJsonContext` | Request/response DTOs for the Weather API |
| `<ServiceName>JsonContext` | DTOs for any other external service |

A good rule of thumb: **one context per Refit interface (or equivalent HTTP
client abstraction)**, plus one context for your own storage models.

### Declaration pattern

```csharp
using System.Text.Json.Serialization;

// Storage context — app's own persisted models
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(UserSettings))]
internal sealed partial class AppStorageJsonContext : JsonSerializerContext
{
}
```

```csharp
using System.Text.Json.Serialization;

// External API context — DTOs owned by the Weather API boundary
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WeatherForecast))]
[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(GeoLocation))]
internal sealed partial class WeatherApiJsonContext : JsonSerializerContext
{
}
```

Each context lives next to the code that uses it — for example:

```
src/AppTemplate/
├── Models/
│   ├── AppConfig.cs
│   ├── UserSettings.cs
│   └── AppStorageJsonContext.cs   ← storage boundary
├── Services/
│   └── Weather/
│       ├── WeatherForecast.cs
│       ├── GeoLocation.cs
│       └── WeatherApiJsonContext.cs  ← Weather API boundary
```

### Registering with `HttpClient` / Refit

Pass the context instance to `JsonSerializerOptions` when configuring your
HTTP client:

```csharp
services.AddHttpClient<IWeatherApi, WeatherApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
})
.AddTypedClient((http, _) =>
    RestService.For<IWeatherApi>(http, new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                TypeInfoResolver = WeatherApiJsonContext.Default
            })
    }));
```

For direct `JsonSerializer` calls use the typed overloads:

```csharp
// Serialize
string json = JsonSerializer.Serialize(forecast, WeatherApiJsonContext.Default.WeatherForecast);

// Deserialize
WeatherForecast? result = JsonSerializer.Deserialize(json, WeatherApiJsonContext.Default.WeatherForecast);
```

### Checklist when adding a new external API

- [ ] Create `<ServiceName>JsonContext.cs` next to the service/Refit interface.
- [ ] Add `[JsonSerializable(typeof(...))]` for every DTO **and** every
      collection variant used directly (e.g. `typeof(MyDto[])`,
      `typeof(List<MyDto>)`).
- [ ] Pass `<ServiceName>JsonContext.Default` as the `TypeInfoResolver` for
      that client — do **not** add the types to an existing context.
- [ ] Do **not** add API DTOs to `AppStorageJsonContext`; keep the two
      concerns separate.

## Versioning

This template uses Nerdbank.GitVersioning. `main` produces `0.X.0-dev.{height}` prerelease builds with a Dev-channel identity that installs side-by-side with the Store version. Stable releases come from `release/v{minor}` branches. See [docs/versioning.md](./docs/versioning.md) for the full model and [docs/versioning-migration.md](./docs/versioning-migration.md) to apply it to an existing app.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
