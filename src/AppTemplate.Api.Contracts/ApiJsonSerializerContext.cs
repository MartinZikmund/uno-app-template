using System.Text.Json.Serialization;

namespace AppTemplate.Api.Contracts;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the API contract DTOs.
/// Shared by the server and the typed client to enable trimming/AOT-friendly,
/// reflection-free JSON (de)serialization.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ItemDto))]
[JsonSerializable(typeof(IReadOnlyList<ItemDto>))]
[JsonSerializable(typeof(CreateItemRequest))]
public partial class ApiJsonSerializerContext : JsonSerializerContext
{
}
