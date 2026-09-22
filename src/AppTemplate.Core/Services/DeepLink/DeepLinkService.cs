namespace AppTemplate.Core.Services.DeepLink;

/// <summary>
/// Thread-safe implementation of <see cref="IDeepLinkService"/>.
/// </summary>
public class DeepLinkService : IDeepLinkService
{
    private readonly object _lock = new();
    private string? _pendingDeepLink;

    public void SetPendingNavigation(string deepLink)
    {
        lock (_lock)
        {
            _pendingDeepLink = deepLink;
        }
    }

    public string? ConsumePendingNavigation()
    {
        lock (_lock)
        {
            var deepLink = _pendingDeepLink;
            _pendingDeepLink = null;
            return deepLink;
        }
    }

    public bool HasPendingNavigation
    {
        get
        {
            lock (_lock)
            {
                return _pendingDeepLink is not null;
            }
        }
    }
}
