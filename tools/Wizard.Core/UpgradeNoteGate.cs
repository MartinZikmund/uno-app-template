using System.Text.RegularExpressions;

namespace MZikmund.UnoApp.Wizard;

public sealed record UpgradeNoteVerdict(bool Passed, string Message, IReadOnlyList<string> ShippingFiles);

/// <summary>The CI rule: a change that reaches apps has to tell those apps how to follow along.</summary>
public static partial class UpgradeNoteGate
{
    // Paths apps never receive, plus docs: a wording fix isn't something an app has to act on.
    private static readonly string[] _nonShippingPrefixes =
    [
        "docs/", "tools/", "tests/Template.", "scripts/", ".template.config/",
        ".github/workflows/template-", ".github/FUNDING.yml",
        ".claude/rules/template-", ".claude/skills/template-",
        "README.md", "LICENSE", "template/overlay/README.md", "template/overlay/docs/",
    ];

    public static bool Ships(string path)
    {
        var normalized = path.Replace('\\', '/');

        // It doesn't ship itself, but it decides what every generated app contains.
        return normalized == ".template.config/template.json"
            || !_nonShippingPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.Ordinal));
    }

    public static bool IsUpgradeNote(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.StartsWith("docs/upgrades/", StringComparison.Ordinal)
            && normalized.EndsWith(".md", StringComparison.Ordinal)
            && !normalized.EndsWith("/README.md", StringComparison.Ordinal)
            && !normalized.EndsWith("/_TEMPLATE.md", StringComparison.Ordinal);
    }

    /// <param name="addedFiles">Only a new note counts: touching an old one says nothing about this change.</param>
    public static UpgradeNoteVerdict Evaluate(IEnumerable<string> changedFiles, IEnumerable<string> addedFiles, string? pullRequestBody)
    {
        var files = changedFiles.ToList();
        var shipping = files.Where(Ships).ToList();

        if (shipping.Count == 0)
        {
            return new UpgradeNoteVerdict(true, "Nothing here reaches apps, so no upgrade note is needed.", shipping);
        }

        if (addedFiles.Any(IsUpgradeNote))
        {
            return new UpgradeNoteVerdict(true, "This change comes with an upgrade note.", shipping);
        }

        var waiver = pullRequestBody is null ? null : WaiverPattern().Match(pullRequestBody);
        if (waiver is { Success: true })
        {
            return new UpgradeNoteVerdict(true, $"Upgrade note waived: {waiver.Groups[1].Value.Trim()}", shipping);
        }

        return new UpgradeNoteVerdict(
            false,
            "This change reaches apps created from the template but has no upgrade note. Add docs/upgrades/YYYY-MM-DD-short-slug.md " +
            "(see .claude/rules/template-upgrades.md). If an app really has nothing to do, add a line to the PR description: " +
            "'Upgrade-Note: none - <reason>'.",
            shipping);
    }

    // A reason is mandatory, so the waiver can't turn into a reflex.
    // [ \t], not \s, or a waiver with no reason could borrow the next line of the description as one.
    [GeneratedRegex(@"^[ \t]*Upgrade-Note:[ \t]*none[ \t]*[-–—:][ \t]*(\S.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex WaiverPattern();
}
