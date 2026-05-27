namespace AppTemplate.Core.Infrastructure;

/// <summary>
/// Release-time constants that are baked into a build rather than configured per environment.
/// </summary>
/// <remarks>
/// Use this for values that are fixed for a given release (build channel, support contacts,
/// legal URLs). For values that vary per environment and are bound from configuration files,
/// see <see cref="AppTemplate.Core.Configuration.AppConfig"/>.
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
    /// <remarks>Placeholder value &mdash; replace with your app's real feedback address before release.</remarks>
    public const string FeedbackEmail = "feedback@example.com";

    /// <summary>
    /// The URL of the application's privacy policy.
    /// </summary>
    /// <remarks>Placeholder value &mdash; replace with your app's real privacy policy URL before release.</remarks>
    public const string PrivacyPolicyUrl = "https://example.com/privacy";

    /// <summary>
    /// The marketing/distribution channel for this build (for example, <c>Store</c>, <c>Sideload</c>, or <c>Beta</c>).
    /// </summary>
    /// <remarks>Placeholder value &mdash; set per release/build channel before publishing.</remarks>
    public const string MarketingChannel = "Store";
}
