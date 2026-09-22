namespace AppTemplate.Infrastructure;

public static partial class AppEnvironment
{
#if APP_CHANNEL_DEV
    public const bool IsDevChannel = true;
    public const string ChannelLabel = "DEV";
#else
    public const bool IsDevChannel = false;
    public const string ChannelLabel = "";
#endif

    /// <summary>
    /// Text for the title-bar channel badge: the channel label, plus the worktree when this build
    /// came from one (<c>DEV · identity</c>). <see cref="WorktreeName"/> is supplied by the
    /// generated part of this class.
    /// </summary>
    public static string ChannelBadgeLabel =>
        WorktreeName is { Length: > 0 } worktree ? $"{ChannelLabel} · {worktree}" : ChannelLabel;
}
