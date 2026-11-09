using System.Text;
using System.Text.RegularExpressions;

namespace MZikmund.UnoApp.Wizard;

/// <summary>
/// The choices made so far. Switching a feature on pulls in what it requires, and switching off a feature
/// that something still needs is refused, so an impossible selection can't be represented.
/// </summary>
public sealed partial class WizardSelection
{
    private const string _displayTextRule = "must start with a letter or digit, then use only letters, digits, spaces and . _ , ( ) ! + -";

    private readonly TemplateManifest _manifest;
    private readonly HashSet<string> _platforms;
    private readonly HashSet<string> _features;
    private readonly Dictionary<string, string> _parameters = new(StringComparer.Ordinal);

    public WizardSelection(TemplateManifest manifest)
    {
        _manifest = manifest;
        _platforms = [.. manifest.Platforms.Select(platform => platform.Id)];
        _features = [.. manifest.Features.Where(feature => feature.DefaultEnabled).Select(feature => feature.Id)];
    }

    public string Name { get; set; } = "";

    public string OutputDirectory { get; set; } = "";

    public IReadOnlySet<string> Platforms => _platforms;

    public IReadOnlySet<string> Features => _features;

    public bool IsEnabled(string feature) => _features.Contains(feature);

    public bool IsPlatformEnabled(string platform) => _platforms.Contains(platform);

    public void SetPlatform(string platform, bool enabled)
    {
        if (_manifest.Platforms.All(p => p.Id != platform))
        {
            throw new ArgumentException($"Unknown platform '{platform}'.", nameof(platform));
        }

        if (enabled)
        {
            _platforms.Add(platform);
        }
        else
        {
            _platforms.Remove(platform);
        }
    }

    public void SetParameter(string id, string value) => _parameters[id] = value;

    public string GetParameter(string id) => _parameters.GetValueOrDefault(id, "");

    /// <summary>Features that are on only because <paramref name="feature"/> or another feature needs them.</summary>
    public IReadOnlyList<string> RequiredBy(string feature) =>
        [.. _manifest.Requires
            .Where(entry => _features.Contains(entry.Key) && entry.Value.Contains(feature))
            .Select(entry => entry.Key)];

    /// <returns><see langword="false"/> when something still requires the feature, which then stays on.</returns>
    public bool SetFeature(string feature, bool enabled)
    {
        if (_manifest.Features.All(f => f.Id != feature))
        {
            throw new ArgumentException($"Unknown feature '{feature}'.", nameof(feature));
        }

        if (enabled)
        {
            _features.Add(feature);
            foreach (var required in _manifest.Requires.GetValueOrDefault(feature, []))
            {
                SetFeature(required, true);
            }

            return true;
        }

        if (RequiredBy(feature).Count > 0)
        {
            return false;
        }

        _features.Remove(feature);
        return true;
    }

    /// <summary>
    /// Replaces the whole feature set at once, so order can't matter. A feature that was left out but is needed by
    /// one that wasn't stays on; those are returned so the caller can say so.
    /// </summary>
    public IReadOnlyList<string> SetFeatures(IEnumerable<string> wanted)
    {
        var requested = wanted.ToList();
        _features.Clear();
        foreach (var feature in requested)
        {
            SetFeature(feature, true);
        }

        return [.. _features.Except(requested).Order(StringComparer.Ordinal)];
    }

    public IReadOnlyList<string> Validate()
    {
        List<string> problems = [];

        if (string.IsNullOrWhiteSpace(Name))
        {
            problems.Add("Enter a project name.");
        }
        else if (!ProjectNamePattern().IsMatch(Name))
        {
            problems.Add("The project name must start with a letter and use only letters, digits and dots.");
        }
        else if (Name.Equals("AppTemplate", StringComparison.OrdinalIgnoreCase))
        {
            problems.Add("Pick a name other than AppTemplate.");
        }

        if (_platforms.Count == 0)
        {
            problems.Add("Pick at least one platform.");
        }

        var appId = GetParameter("appId");
        if (appId.Length > 0 && !AppIdPattern().IsMatch(appId))
        {
            problems.Add("The application id must look like com.contoso.myapp: lowercase segments separated by dots.");
        }

        // Both end up in XML, JSON, MSBuild and Android resources, each with its own special characters, unescaped.
        var title = GetParameter("title");
        if (title.Length > 0 && !DisplayTextPattern().IsMatch(title))
        {
            problems.Add($"The display name {_displayTextRule}");
        }

        if (title.Length > 36)
        {
            problems.Add("The display name can be at most 36 characters, because Windows limits the Dev name to 40.");
        }
        else if (title.Length == 0 && Name.Length > 36)
        {
            problems.Add("The display name defaults to the project name, which is over the 36-character limit Windows sets. Enter a shorter display name.");
        }

        var publisher = GetParameter("publisher");
        if (publisher.Length > 0 && !DisplayTextPattern().IsMatch(publisher))
        {
            problems.Add($"The publisher {_displayTextRule}");
        }

        var repo = GetParameter("githubRepo");
        if (repo.Length > 0 && !GitHubRepoPattern().IsMatch(repo))
        {
            problems.Add("The GitHub repository must look like owner/repo.");
        }

        return problems;
    }

    /// <summary>
    /// Arguments for <c>dotnet new</c>. Only what differs from the defaults, so a command stays short enough to read.
    /// </summary>
    public IReadOnlyList<string> ToDotnetNewArguments()
    {
        List<string> arguments = ["new", _manifest.ShortName, "-n", Name];

        if (OutputDirectory.Length > 0)
        {
            arguments.AddRange(["-o", OutputDirectory]);
        }

        foreach (var parameter in _manifest.Parameters)
        {
            var value = GetParameter(parameter.Id);
            if (value.Length > 0 && value != parameter.DefaultValue)
            {
                arguments.AddRange([$"--{parameter.CliName}", value]);
            }
        }

        if (_platforms.Count != _manifest.Platforms.Count)
        {
            foreach (var platform in _manifest.Platforms.Where(p => _platforms.Contains(p.Id)))
            {
                arguments.AddRange(["--platforms", platform.Id]);
            }
        }

        foreach (var feature in _manifest.Features)
        {
            var enabled = _features.Contains(feature.Id);
            if (enabled != feature.DefaultEnabled)
            {
                arguments.AddRange([$"--{feature.CliName}", enabled ? "true" : "false"]);
            }
        }

        return arguments;
    }

    public string ToCommandLine()
    {
        StringBuilder command = new("dotnet");
        foreach (var argument in ToDotnetNewArguments())
        {
            command.Append(' ').Append(Quote(argument));
        }

        return command.ToString();
    }

    private static string Quote(string argument) =>
        argument.Length > 0 && SafeArgumentPattern().IsMatch(argument) ? argument : $"\"{argument.Replace("\"", "\\\"")}\"";

    // \z rather than $ throughout: $ also matches before a trailing newline.
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)*\z")]
    private static partial Regex ProjectNamePattern();

    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+\z")]
    private static partial Regex AppIdPattern();

    [GeneratedRegex(@"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z")]
    private static partial Regex GitHubRepoPattern();

    [GeneratedRegex(@"^[A-Za-z0-9_./:\\=-]+\z")]
    private static partial Regex SafeArgumentPattern();

    [GeneratedRegex(@"^[\p{L}\p{N}][\p{L}\p{N} ._,()!+-]*\z")]
    private static partial Regex DisplayTextPattern();
}

public sealed record CreateRequest(
    string Name,
    string OutputDirectory,
    IReadOnlyList<string> Platforms,
    IReadOnlyList<string> Features,
    IReadOnlyDictionary<string, string> Parameters,
    bool InitializeGit);

public sealed record CreateResponse(bool Success, string Message, string? Path);

/// <param name="CanCreate"><see langword="false"/> in a hosted preview, where nothing can write to the visitor's disk.</param>
public sealed record WizardInfo(string Version, string OutputRoot, bool CanCreate = true);
