namespace AppTemplate.Api.Contracts;

/// <summary>
/// Represents a single item returned by the API.
/// Shared between the API server and the typed client.
/// </summary>
/// <param name="Id">The unique identifier of the item.</param>
/// <param name="Name">The display name of the item.</param>
/// <param name="Description">An optional, longer description of the item.</param>
/// <param name="CreatedAt">The UTC timestamp at which the item was created.</param>
public sealed record ItemDto(
    int Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt);
