// EXAMPLE FILE — copy and adapt this to your own app.
//
// A per-app JsonSerializerContext is required for AOT-safe serialization
// (iOS NativeAOT, WASM trimmed builds). It provides the compiler with the
// closed set of types that may be serialized/deserialized, so no runtime
// reflection is needed.
//
// Steps to adapt:
//   1. Rename this class (e.g. MyAppJsonContext) and update the namespace.
//   2. Replace ExampleModel with your own model types.
//   3. Add one [JsonSerializable] attribute per serializable type (and its
//      collection variants, e.g. List<T>, T[]) that you use via JsonSerializer.
//   4. Pass AppTemplateJsonContext.Default as the TypeInfoResolver when you
//      call JsonSerializer methods:
//
//        var json = JsonSerializer.Serialize(obj,
//            AppTemplateJsonContext.Default.ExampleModel);
//
//        var obj = JsonSerializer.Deserialize(json,
//            AppTemplateJsonContext.Default.ExampleModel);
//
//   5. For helpers that accept JsonSerializerOptions, compose via:
//
//        JsonSerializerOptions options = new()
//        {
//            TypeInfoResolver = AppTemplateJsonContext.Default,
//        };

using System.Text.Json.Serialization;

namespace AppTemplate.Core.Models;

// ---------------------------------------------------------------------------
// Example model — replace with your own domain types.
// ---------------------------------------------------------------------------

/// <summary>Example serializable record. Replace with your own model.</summary>
public partial record ExampleModel
{
    /// <summary>An identifier for the example entity.</summary>
    public int Id { get; init; }

    /// <summary>A display name for the example entity.</summary>
    public string? Name { get; init; }
}

// ---------------------------------------------------------------------------
// Source-generated serializer context
// ---------------------------------------------------------------------------

/// <summary>
/// AOT-safe JSON serializer context for this app.
/// Register every model type (and its collection variants) via
/// <see cref="JsonSerializableAttribute"/> so that no runtime reflection
/// is required on iOS / NativeAOT / trimmed WASM builds.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,          // compact for storage / network; use true for export files
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ExampleModel))]
[JsonSerializable(typeof(List<ExampleModel>))]
public partial class AppTemplateJsonContext : JsonSerializerContext
{
}
