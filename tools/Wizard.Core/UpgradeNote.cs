using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MZikmund.UnoApp.Wizard;

[JsonConverter(typeof(JsonStringEnumConverter<UpgradeImpact>))]
public enum UpgradeImpact
{
    Breaking,
    Recommended,
    Optional,
}

/// <summary>One file in <c>docs/upgrades/</c>: what changed in the template and how an app follows along.</summary>
public sealed partial record UpgradeNote(
    string Id,
    string Title,
    UpgradeImpact Impact,
    IReadOnlyList<string> Features,
    IReadOnlyList<string> Platforms,
    string Body)
{
    private static readonly string[] _requiredSections = ["## Why", "## Who is affected", "## Steps", "## Verify"];

    public static UpgradeNote Parse(string id, string markdown)
    {
        var text = markdown.Replace("\r\n", "\n");
        var match = FrontMatterPattern().Match(text);
        if (!match.Success)
        {
            throw new InvalidDataException($"{id}: an upgrade note must start with --- front matter.");
        }

        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        foreach (var line in match.Groups[1].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = line.IndexOf(':');
            if (colon > 0)
            {
                fields[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            }
        }

        if (!IdPattern().IsMatch(id))
        {
            throw new InvalidDataException($"{id}: the file name must be YYYY-MM-DD-short-slug.md.");
        }

        var title = fields.GetValueOrDefault("title", "").Trim('"');
        if (title.Length == 0)
        {
            throw new InvalidDataException($"{id}: front matter needs a title.");
        }

        if (!fields.TryGetValue("impact", out var impactText) || !Enum.TryParse<UpgradeImpact>(impactText, ignoreCase: true, out var impact))
        {
            throw new InvalidDataException($"{id}: front matter needs impact: breaking, recommended or optional.");
        }

        var body = text[match.Length..];
        var missing = _requiredSections.Where(section => !body.Contains(section, StringComparison.Ordinal)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidDataException($"{id}: missing section(s): {string.Join(", ", missing)}.");
        }

        return new UpgradeNote(id, title, impact, ParseList(fields, "features"), ParseList(fields, "platforms"), body.Trim());
    }

    /// <summary>Does this note concern the app? An empty list means "every app".</summary>
    public bool AppliesTo(TemplateState state)
    {
        var featureMatch = Features.Count == 0 || Features.Any(state.HasOption);
        var platformMatch = Platforms.Count == 0 || Platforms.Any(state.Platforms.Contains);
        return featureMatch && platformMatch;
    }

    private static IReadOnlyList<string> ParseList(Dictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var value)
            ? [.. value.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]
            : [];

    [GeneratedRegex(@"^---\n(.*?)\n---\n", RegexOptions.Singleline)]
    private static partial Regex FrontMatterPattern();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}-[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex IdPattern();
}

public static class UpgradePlanner
{
    /// <summary>What this app still has to do, oldest first. Ids sort by date because they start with one.</summary>
    public static IReadOnlyList<UpgradeNote> Pending(IEnumerable<UpgradeNote> catalog, TemplateState state) =>
        [.. catalog
            .Where(note => !state.AppliedUpgrades.Contains(note.Id, StringComparer.Ordinal))
            .Where(note => note.AppliesTo(state))
            .OrderBy(note => note.Id, StringComparer.Ordinal)];
}
