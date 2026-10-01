namespace AppTemplate.Infrastructure;

public static partial class AppEnvironment
{
#if APP_CHANNEL_DEV
    public const bool IsDevChannel = true;
#else
    public const bool IsDevChannel = false;
#endif

#if APP_CHANNEL_DEV && APP_CI_BUILD
    public const string ChannelLabel = "CI";
#elif APP_CHANNEL_DEV
    public const string ChannelLabel = "DEV";
#else
    public const string ChannelLabel = "";
#endif

    /// <summary>
    /// A Dev build made on CI (e.g. a staging deployment). Its badges read "CI" in blue instead of "DEV", matching the
    /// app icon; see docs/dev-assets.md.
    /// </summary>
    public static bool IsCiBuild { get; } = ChannelLabel == "CI";

    /// <summary>
    /// Text for the title-bar channel badge: the channel label, plus the worktree when this build
    /// came from one (<c>DEV · identity</c>). <see cref="WorktreeName"/> is supplied by the
    /// generated part of this class.
    /// </summary>
    public static string ChannelBadgeLabel =>
        WorktreeName is { Length: > 0 } worktree ? $"{ChannelLabel} · {worktree}" : ChannelLabel;
}
