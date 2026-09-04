// Source-generated JSON serializer context for AppTemplate.
//
// A per-app JsonSerializerContext is required for AOT-safe serialization
// (iOS NativeAOT, trimmed WASM builds). It gives the compiler the closed set of
// types that may be serialized/deserialized, so no runtime reflection is needed.
//
// To add a serializable type:
//   1. Add one [JsonSerializable] attribute per type, plus the collection
//      variants (List<T>, T[]) that get passed to JsonSerializer.
//   2. Serialize/deserialize through the generated metadata property — this is
//      the AOT-safe call shape:
//
//        using System.Text.Json;
//
//        var json = JsonSerializer.Serialize(
//            value, AppTemplateJsonContext.Default.ExampleModel);
//
//        var value = JsonSerializer.Deserialize(
//            json, AppTemplateJsonContext.Default.ExampleModel);

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppTemplate.Core.Models;

/// <summary>Sample serializable record demonstrating the context registration.</summary>
public partial record ExampleModel
{
    /// <summary>An identifier for the entity.</summary>
    public int Id { get; init; }

    /// <summary>A display name for the entity.</summary>
    public string? Name { get; init; }
}

/// <summary>
/// AOT-safe JSON serializer context for the app. Register every model type
/// (and its collection variants) via <see cref="JsonSerializableAttribute"/> so
/// that no runtime reflection is required on iOS / NativeAOT / trimmed WASM builds.
/// </summary>
[JsonSerializable(typeof(ExampleModel))]
[JsonSerializable(typeof(ExampleModel[]))]
[JsonSerializable(typeof(List<ExampleModel>))]
public partial class AppTemplateJsonContext : JsonSerializerContext
{
    // The source generator provides the Default property.
}
