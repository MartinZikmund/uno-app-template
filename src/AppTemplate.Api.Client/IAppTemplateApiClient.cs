using AppTemplate.Api.Contracts;
using Refit;

namespace AppTemplate.Api.Client;

/// <summary>
/// Typed client for the AppTemplate API, backed by Refit.
/// Register it via <see cref="ServiceCollectionExtensions.AddAppTemplateApiClient"/>.
/// </summary>
public interface IAppTemplateApiClient
{
    /// <summary>
    /// Gets all items.
    /// </summary>
    [Get("/api/v1/items")]
    Task<IReadOnlyList<ItemDto>> GetItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single item by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the item to retrieve.</param>
    [Get("/api/v1/items/{id}")]
    Task<ItemDto> GetItemAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new item.
    /// </summary>
    /// <param name="request">The item creation payload.</param>
    [Post("/api/v1/items")]
    Task<ItemDto> CreateItemAsync([Body] CreateItemRequest request, CancellationToken cancellationToken = default);
}
