using System.Text.Json;

namespace MZikmund.UnoApp.Wizard;

/// <summary>The contents of an app's <c>.template-state.json</c>.</summary>
public sealed record TemplateState(
    string Template,
    string TemplateVersion,
    string? UpgradesUrl,
    string Name,
    IReadOnlyDictionary<string, string> Options,
    IReadOnlyList<string> AppliedUpgrades)
{
    public const string FileName = ".template-state.json";

    public const string TemplatePackageId = "MZikmund.UnoApp.Templates";

    public const string DefaultUpgradesUrl = "https://github.com/MartinZikmund/uno-app-template/tree/main/docs/upgrades";

    public static TemplateState Parse(string json) =>
        JsonSerializer.Deserialize(json, WizardJsonContext.Default.TemplateState)
        ?? throw new InvalidDataException($"{FileName} is empty.");

    public string ToJson() => JsonSerializer.Serialize(this, WizardJsonContext.Default.TemplateState) + Environment.NewLine;

    /// <summary>Compares the numeric part only. Prerelease suffixes here are commit hashes, which have no order.</summary>
    public static bool IsOlder(string version, string other) =>
        System.Version.TryParse(version.Split('-')[0], out var left)
        && System.Version.TryParse(other.Split('-')[0], out var right)
        && left < right;

    public bool HasOption(string option) =>
        Options.TryGetValue(option, out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    public IReadOnlySet<string> Platforms =>
        Options.TryGetValue("platforms", out var value)
            ? value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>();

    public TemplateState MarkApplied(IEnumerable<string> ids) =>
        this with { AppliedUpgrades = [.. AppliedUpgrades.Union(ids).Order(StringComparer.Ordinal)] };

    /// <summary>Arguments that regenerate this app's options with <c>dotnet new</c>, in the manifest's vocabulary.</summary>
    public IReadOnlyList<string> ToDotnetNewOptionArguments(TemplateManifest manifest)
    {
        List<string> arguments = [];
        foreach (var platform in Platforms.Order(StringComparer.Ordinal))
        {
            arguments.AddRange(["--platforms", platform]);
        }

        foreach (var option in Options)
        {
            if (option.Key == "platforms")
            {
                continue;
            }

            // A feature this manifest doesn't know came from a different template version.
            var feature = manifest.FindFeature(option.Key);
            if (feature is not null)
            {
                arguments.AddRange([$"--{feature.CliName}", option.Value.ToLowerInvariant()]);
            }
            else if (manifest.Parameters.FirstOrDefault(p => p.Id == option.Key) is { } parameter && option.Value.Length > 0)
            {
                arguments.AddRange([$"--{parameter.CliName}", option.Value]);
            }
        }

        return arguments;
    }
}
