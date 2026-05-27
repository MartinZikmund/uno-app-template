namespace AppTemplate.Core.Infrastructure;

/// <summary>
/// Abstracts the application-level surface that Core code depends on, decoupling it
/// from the concrete <c>Microsoft.UI.Xaml.Application</c> type.
/// </summary>
/// <remarks>
/// Keeping Core dependent on this minimal abstraction (rather than the UI framework's
/// <c>Application</c> singleton) lets the same Core logic be hosted in environments that
/// do not provide a real WinUI application instance &#8212; for example unit/integration
/// tests, server-side scenarios, or alternative renderers. Implementations are typically
/// registered in DI as a singleton (e.g. <c>services.AddSingleton&lt;IApplication&gt;(sp =&gt; App.Current)</c>)
/// so consumers depend only on this interface.
/// </remarks>
public interface IApplication
{
    /// <summary>
    /// Gets the theme requested for the application, used as the baseline when an element
    /// does not specify an explicit theme.
    /// </summary>
    ApplicationTheme RequestedTheme { get; }

    /// <summary>
    /// Gets the application-scoped resource dictionary, providing access to globally
    /// available resources such as styles, brushes, and templates.
    /// </summary>
    ResourceDictionary Resources { get; }

    /// <summary>
    /// Gets the display version of the application.
    /// </summary>
    string AppVersion { get; }

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    void Exit();
}
