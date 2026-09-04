# JSON serialization (AOT / iOS / NativeAOT)

## Why a per-app `JsonSerializerContext`

Reflection-based JSON serialization (`JsonSerializer.Serialize<T>(value)` with no
extra options) fails at runtime on platforms that disallow runtime code generation,
most notably **iOS NativeAOT** and trimmed **WASM** builds. The .NET trimmer cannot
statically determine which types will be serialized, so the required metadata is
stripped away.

The fix is **source-generated serialization** via
[`JsonSerializerContext`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation).
A `partial` class is annotated with `[JsonSerializable(typeof(ExampleModel))]` for
every type the app serializes, and the compiler generates all required type metadata
at build time. No runtime reflection is needed.

The context lives in the app itself rather than in a shared library or NuGet package,
because it must declare the closed set of types known at compile time — and those
types are specific to this app.

## `JsonSourceGenerationOptions` conventions

The options on the context depend on what the JSON is for:

| Use case | `WriteIndented` | Notes |
|---|---|---|
| **Storage / network** | `false` (the default) | Compact; smaller payloads, faster I/O. |
| **Export / debug files** | `true` | Human-readable; easier to inspect and diff. |

Keep the rest at the System.Text.Json defaults unless a model needs something
specific — apply per-type or per-property attributes (for example
`[JsonPropertyName]`, `[JsonIgnore]`) on the models themselves rather than forcing a
global policy onto the whole context.

## Registering types in the context

`AppTemplateJsonContext` (in `src/AppTemplate.Core/Models/`) is the source-generated
context. Add one `[JsonSerializable]` attribute per serializable type, including the
collection variants (`List<T>`, `T[]`, …) that get passed to `JsonSerializer`:

```csharp
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppTemplate.Core.Models;

[JsonSerializable(typeof(ExampleModel))]
[JsonSerializable(typeof(ExampleModel[]))]
[JsonSerializable(typeof(List<ExampleModel>))]
public partial class AppTemplateJsonContext : JsonSerializerContext
{
    // The source generator provides the Default property.
}
```

## Using the context

Pass the generated metadata property (`Default.<TypeName>`) directly to
`JsonSerializer` — this is the AOT-safe call shape, with no `JsonSerializerOptions`
overload that would fall back to reflection:

```csharp
using System.Text.Json;

// Serialize
string json = JsonSerializer.Serialize(model, AppTemplateJsonContext.Default.ExampleModel);

// Deserialize
ExampleModel? value = JsonSerializer.Deserialize(json, AppTemplateJsonContext.Default.ExampleModel);
```

For third-party helpers that only accept `JsonSerializerOptions`, expose the context
through a resolver:

```csharp
JsonSerializerOptions options = new()
{
    TypeInfoResolver = AppTemplateJsonContext.Default,
};
```
