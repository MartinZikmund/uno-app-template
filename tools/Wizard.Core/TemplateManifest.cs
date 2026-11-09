using System.Text.Json;
using System.Text.Json.Serialization;

namespace MZikmund.UnoApp.Wizard;

/// <summary>What the wizard can offer: read from the template's own config, so there is one source of truth.</summary>
public sealed record TemplateManifest(
    string ShortName,
    IReadOnlyList<ManifestChoice> Platforms,
    IReadOnlyList<ManifestParameter> Parameters,
    IReadOnlyList<ManifestGroup> Groups,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Requires)
{
    public IEnumerable<ManifestFeature> Features => Groups.SelectMany(group => group.Features);

    public ManifestFeature? FindFeature(string id) =>
        Features.FirstOrDefault(feature => string.Equals(feature.Id, id, StringComparison.OrdinalIgnoreCase)
            || string.Equals(feature.CliName, id, StringComparison.OrdinalIgnoreCase));

    public static TemplateManifest Parse(string templateJson, string cliHostJson, string wizardJson)
    {
        JsonDocumentOptions options = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        using var template = JsonDocument.Parse(templateJson, options);
        using var cliHost = JsonDocument.Parse(cliHostJson, options);
        using var wizard = JsonDocument.Parse(wizardJson, options);

        var symbols = template.RootElement.GetProperty("symbols");
        var cliNames = cliHost.RootElement.GetProperty("symbolInfo");

        List<ManifestChoice> platforms = [];
        foreach (var choice in symbols.GetProperty("platforms").GetProperty("choices").EnumerateArray())
        {
            platforms.Add(new ManifestChoice(
                choice.GetProperty("choice").GetString()!,
                choice.GetProperty("displayName").GetString()!,
                choice.GetProperty("description").GetString()!));
        }

        List<ManifestParameter> parameters = [];
        foreach (var symbol in symbols.EnumerateObject())
        {
            if (symbol.Value.GetProperty("type").GetString() == "parameter"
                && symbol.Value.TryGetProperty("datatype", out var datatype)
                && datatype.GetString() == "string")
            {
                parameters.Add(new ManifestParameter(
                    symbol.Name,
                    CliName(cliNames, symbol.Name),
                    symbol.Value.GetProperty("displayName").GetString()!,
                    symbol.Value.GetProperty("description").GetString()!,
                    symbol.Value.GetProperty("defaultValue").GetString()!));
            }
        }

        List<ManifestGroup> groups = [];
        foreach (var group in wizard.RootElement.GetProperty("groups").EnumerateArray())
        {
            List<ManifestFeature> features = [];
            foreach (var id in group.GetProperty("features").EnumerateArray().Select(e => e.GetString()!))
            {
                if (!symbols.TryGetProperty(id, out var symbol))
                {
                    throw new InvalidDataException($"wizard.json lists '{id}', which template.json doesn't define.");
                }

                features.Add(new ManifestFeature(
                    id,
                    CliName(cliNames, id),
                    symbol.GetProperty("displayName").GetString()!,
                    symbol.GetProperty("description").GetString()!,
                    string.Equals(symbol.GetProperty("defaultValue").GetString(), "true", StringComparison.OrdinalIgnoreCase)));
            }

            groups.Add(new ManifestGroup(
                group.GetProperty("id").GetString()!,
                group.GetProperty("title").GetString()!,
                group.GetProperty("description").GetString()!,
                group.GetProperty("icon").GetString()!,
                features));
        }

        Dictionary<string, IReadOnlyList<string>> requires = [];
        foreach (var entry in wizard.RootElement.GetProperty("requires").EnumerateObject())
        {
            requires[entry.Name] = [.. entry.Value.EnumerateArray().Select(e => e.GetString()!)];
        }

        var listed = groups.SelectMany(group => group.Features).Select(feature => feature.Id).ToHashSet();
        foreach (var symbol in symbols.EnumerateObject())
        {
            if (symbol.Value.GetProperty("type").GetString() == "parameter"
                && symbol.Value.TryGetProperty("datatype", out var datatype)
                && datatype.GetString() == "bool"
                && !listed.Contains(symbol.Name))
            {
                throw new InvalidDataException($"template.json defines the feature '{symbol.Name}', but wizard.json doesn't list it.");
            }
        }

        return new TemplateManifest(template.RootElement.GetProperty("shortName").GetString()!, platforms, parameters, groups, requires);
    }

    private static string CliName(JsonElement cliNames, string symbol) =>
        cliNames.TryGetProperty(symbol, out var info) ? info.GetProperty("longName").GetString()! : symbol;
}

public sealed record ManifestChoice(string Id, string DisplayName, string Description);

public sealed record ManifestParameter(string Id, string CliName, string DisplayName, string Description, string DefaultValue);

public sealed record ManifestFeature(string Id, string CliName, string DisplayName, string Description, bool DefaultEnabled);

public sealed record ManifestGroup(string Id, string Title, string Description, string Icon, IReadOnlyList<ManifestFeature> Features);

[JsonSerializable(typeof(TemplateManifest))]
[JsonSerializable(typeof(CreateRequest))]
[JsonSerializable(typeof(CreateResponse))]
[JsonSerializable(typeof(TemplateState))]
[JsonSerializable(typeof(WizardInfo))]
[JsonSerializable(typeof(IReadOnlyList<UpgradeNote>))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true)]
public sealed partial class WizardJsonContext : JsonSerializerContext;
