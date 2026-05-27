namespace AppTemplate.Core.Configuration;

/// <summary>
/// Release-time constants baked into a build rather than configured per environment.
/// </summary>
/// <remarks>
/// These values are fixed for a given release (data schema version, build channel, support
/// contacts, legal URLs). For values that vary per environment and are bound from configuration
/// files, see <see cref="AppConfig"/>.
/// </remarks>
public static class ApplicationReleaseInfo
{
    /// <summary>
    /// The current application data schema version. Increment this when introducing a migration
    /// that must run on existing installations.
    /// </summary>
    public const int DataVersion = 1;

    /// <summary>
    /// The email address used to collect user feedback and support requests.
    /// </summary>
    public const string FeedbackEmail = "feedback@example.com";

    /// <summary>
    /// The URL of the application's privacy policy.
    /// </summary>
    public const string PrivacyPolicyUrl = "https://example.com/privacy";

    /// <summary>
    /// The distribution channel for this build (for example, <c>Store</c>, <c>Sideload</c>, or <c>Beta</c>).
    /// </summary>
    public const string MarketingChannel = "Store";
}
