# One JsonSerializerContext per boundary

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

## Why one context per boundary?

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

## Logical boundaries

| Context | Lives in | Owns |
|---------|----------|------|
| `AppTemplateJsonContext` (illustrative — not present in this template) | the app / `AppTemplate.Core` | The app's own models and persisted/export shapes |
| `<Service>JsonSerializerContext` | that service's client project | Request/response DTOs for one external API |

Rule of thumb: **one context per external API client** — a Refit interface set
(shown below purely as an example; the template doesn't depend on Refit) or any
equivalent HTTP abstraction — plus a context for the app's own models. A
dedicated API client is usually its own project (for example
`AppTemplate.<Service>.Api`), and its context sits next to that client's models.

## Declaration pattern

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

## Registering with an HTTP client (Refit example)

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

## Checklist when adding a new external API

- [ ] Add a `<Service>JsonSerializerContext` next to that client's models
      (in the client's own project where there is one).
- [ ] Add `[JsonSerializable(typeof(...))]` for every DTO **and** every
      collection variant used directly (for example `typeof(MyDto[])`,
      `typeof(List<MyDto>)`, paged-response wrappers).
- [ ] Set `[JsonSourceGenerationOptions]` to match the API's wire format
      (`PropertyNamingPolicy`, `DefaultIgnoreCondition`).
- [ ] Wire `<Service>JsonSerializerContext.Default` into the client's
      `RefitSettings` — do **not** add the DTOs to `AppTemplateJsonContext`.
