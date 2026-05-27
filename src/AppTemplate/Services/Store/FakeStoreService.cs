namespace AppTemplate.Services.Store;

/// <summary>
/// In-memory <see cref="IStoreService"/> used for DEBUG builds and on platforms without a real
/// store backend, so that dependency injection always has something to resolve and paywall UI
/// can be exercised without a live store connection.
/// </summary>
public sealed class FakeStoreService : IStoreService
{
    private bool _isPro;

    /// <inheritdoc />
    public Task<bool> TryPurchaseProAsync()
    {
        _isPro = true;
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> TryRestorePurchasesAsync() => Task.FromResult(_isPro);

    /// <inheritdoc />
    public Task<string?> GetPriceAsync() => Task.FromResult<string?>("$9.99");
}
