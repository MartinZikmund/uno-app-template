namespace AppTemplate.Core.Infrastructure;

public interface IApplication
{
    ApplicationTheme RequestedTheme { get; }

    ResourceDictionary Resources { get; }

    string AppVersion { get; }

    /// <summary>
    /// Name of the git worktree this build came from, or <see langword="null"/> for the main
    /// checkout. Dev-channel worktree builds carry their own package identity so they can run
    /// side by side; this is how the app reports which one it is.
    /// </summary>
    string? WorktreeName { get; }

    void Exit();
}
