namespace AppTemplate.Api.Contracts;

/// <summary>
/// Represents the payload used to create a new item.
/// </summary>
/// <param name="Name">The display name of the item to create.</param>
/// <param name="Description">An optional, longer description of the item.</param>
public sealed record CreateItemRequest(
    string Name,
    string? Description);
