namespace AppTemplate.Core.Services.DeepLink;

/// <summary>
/// Handles deep-link navigation requests originating from notifications and other
/// external entry points (for example, a tapped notification or a custom URI scheme).
/// </summary>
/// <remarks>
/// A deep link can arrive before the UI is ready to act on it (notably during a
/// cold start). The service stores the pending link so it can be consumed once the
/// app's navigation is initialized.
/// </remarks>
public interface IDeepLinkService
{
    /// <summary>
    /// Stores a deep link to be navigated to once the app is ready.
    /// Called when the app is activated from a notification or external link.
    /// </summary>
    /// <param name="deepLink">The deep link to navigate to.</param>
    void SetPendingNavigation(string deepLink);

    /// <summary>
    /// Consumes and returns the pending deep link, clearing it in the process.
    /// </summary>
    /// <returns>The pending deep link, or <see langword="null"/> if none is pending.</returns>
    string? ConsumePendingNavigation();

    /// <summary>
    /// Gets a value indicating whether a deep link is currently pending.
    /// </summary>
    bool HasPendingNavigation { get; }
}
