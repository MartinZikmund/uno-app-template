using System.Collections.Concurrent;
using AppTemplate.Api.Contracts;

namespace AppTemplate.Api;

/// <summary>
/// A tiny in-memory store of items, seeded with sample data.
/// Replace with a real data source (database, external service, ...) in your app.
/// </summary>
public sealed class ItemStore
{
    private readonly ConcurrentDictionary<int, ItemDto> _items = new();
    private int _nextId;

    public ItemStore()
    {
        Add(new CreateItemRequest("First item", "An example item seeded at startup."));
        Add(new CreateItemRequest("Second item", "Another example item."));
    }

    public IReadOnlyList<ItemDto> GetAll() =>
        _items.Values.OrderBy(item => item.Id).ToList();

    public ItemDto? Get(int id) =>
        _items.TryGetValue(id, out var item) ? item : null;

    public ItemDto Add(CreateItemRequest request)
    {
        int id = Interlocked.Increment(ref _nextId);
        ItemDto item = new(id, request.Name, request.Description, DateTimeOffset.UtcNow);
        _items[id] = item;
        return item;
    }
}
