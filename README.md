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

> This section documents a **recommended convention** for apps built from this
> template — the template itself does not yet define any `JsonSerializerContext`
> or external API client. Names like `AppTemplateJsonContext`,
> `WeatherJsonSerializerContext`, `IWeatherApi`, `WeatherForecast`, and
> `GeoLocation` below are **illustrative placeholders**, not code that ships in
> this repo; substitute your own models and API when you add one.

Give every **external** API the app talks to its **own**
[`JsonSerializerContext`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonserializercontext),
living alongside that API's client code — kept separate from whatever context
covers the app's own models (for example an `AppTemplateJsonContext`, if the app
has one). This keeps each set of source-generated serialization metadata bounded
to a single boundary instead of piling every unrelated DTO into one context.

### Why one context per boundary?

Source-generated serialization contexts are **zero-cost at runtime** — the
serialization metadata is emitted by the compiler, not reflected at startup.
The cost is paid at **build time** (code generation).

A single monolithic context that lists every DTO across the codebase couples
unrelated shapes together: an external API's request/response models often need
a different `PropertyNamingPolicy` (snake_case, camelCase) or
`DefaultIgnoreCondition` than the app's own models, and folding them into one
context forces a single global policy onto all of them.

Keeping contexts scoped to one boundary means:

- Each context carries its own `[JsonSourceGenerationOptions]` — the naming
  policy and ignore conditions that match *that* API's wire format.
- Each context is independently evolvable; the third-party API changing a field
  cannot affect how the app's own models serialize.
- AOT / NativeAOT / iOS builds stay compliant: the closed set of types required
  by each context is always explicitly declared.

### Logical boundaries

| Context | Lives in | Owns |
|---------|----------|------|
| `AppTemplateJsonContext` (illustrative — not present in this template) | the app / `AppTemplate.Core` | The app's own models and persisted/export shapes |
| `<Service>JsonSerializerContext` | that service's client project | Request/response DTOs for one external API |

Rule of thumb: **one context per external API client** — a Refit interface set
(shown below purely as an example; the template doesn't depend on Refit) or any
equivalent HTTP abstraction — plus a context for the app's own models. A
dedicated API client is usually its own project (for example
`AppTemplate.<Service>.Api`), and its context sits next to that client's models.

### Declaration pattern

An external-API context declares every DTO it serializes, then sets the options
that match the API's wire format. Put `[JsonSourceGenerationOptions]` after the
`[JsonSerializable]` list and make the context `public` — it is consumed from the
client project that owns it. The example below uses a hypothetical Weather API
(`WeatherForecast`, `GeoLocation`) purely to illustrate the pattern — none of
these types exist in this template; substitute your own DTOs:

```csharp
using System.Text.Json.Serialization;

namespace AppTemplate.Weather.Api.Models;

// External API context — DTOs owned by the Weather API client
[JsonSerializable(typeof(WeatherForecast))]
[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(GeoLocation))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class WeatherJsonSerializerContext : JsonSerializerContext
{
}
```

If the app has a context for its own models (an `AppTemplateJsonContext` or
similar), keep external API DTOs out of it — each boundary gets its own context.

A typical layout keeps each external client and its context in a dedicated
project. `AppTemplate.Weather.Api` below is a placeholder name for illustration
— this template ships no such project:

```
src/
├── AppTemplate.Core/
│   └── Models/
│       └── AppTemplateJsonContext.cs        ← the app's own models (if present)
└── AppTemplate.Weather.Api/                 ← one external API = one client project
    ├── Models/
    │   ├── WeatherForecast.cs
    │   ├── GeoLocation.cs
    │   └── WeatherJsonSerializerContext.cs  ← Weather API boundary
    └── Extensions/
        └── ServiceCollectionExtensions.cs   ← Refit registration
```

### Registering with an HTTP client (Refit example)

The template doesn't depend on [Refit](https://github.com/reficted/refit) — it's
used here only as a familiar example of wiring a generated context into an HTTP
client; swap in whatever REST abstraction the app actually uses.

Pass the context's generated `Options` straight to `SystemTextJsonContentSerializer`
so the `[JsonSourceGenerationOptions]` declared on the context (naming policy,
ignore conditions, …) are honored — building a fresh `JsonSerializerOptions` and
only adding the context to its `TypeInfoResolverChain` drops those options.
Requires the `Refit` and `Refit.HttpClientFactory` packages, plus:

```csharp
using System;
using Refit;
```

```csharp
var refitSettings = new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(
        WeatherJsonSerializerContext.Default.Options)
};

services.AddRefitClient<IWeatherApi>(refitSettings)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://api.example.com/"));
```

For direct `JsonSerializer` calls use the typed overloads:

```csharp
// Serialize
string json = JsonSerializer.Serialize(forecast, WeatherJsonSerializerContext.Default.WeatherForecast);

// Deserialize
WeatherForecast? result = JsonSerializer.Deserialize(json, WeatherJsonSerializerContext.Default.WeatherForecast);
```

### Checklist when adding a new external API

- [ ] Add a `<Service>JsonSerializerContext` next to that client's models
      (in the client's own project where there is one).
- [ ] Add `[JsonSerializable(typeof(...))]` for every DTO **and** every
      collection variant used directly (for example `typeof(MyDto[])`,
      `typeof(List<MyDto>)`, paged-response wrappers).
- [ ] Set `[JsonSourceGenerationOptions]` to match the API's wire format
      (`PropertyNamingPolicy`, `DefaultIgnoreCondition`).
- [ ] Wire `<Service>JsonSerializerContext.Default` into the client's
      `RefitSettings` — do **not** add the DTOs to `AppTemplateJsonContext`.

## Versioning

This template uses Nerdbank.GitVersioning. `main` produces `0.X.0-dev.{height}` prerelease builds with a Dev-channel identity that installs side-by-side with the Store version. Stable releases come from `release/v{minor}` branches. See [docs/versioning.md](./docs/versioning.md) for the full model and [docs/versioning-migration.md](./docs/versioning-migration.md) to apply it to an existing app.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
