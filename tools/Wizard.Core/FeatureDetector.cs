namespace MZikmund.UnoApp.Wizard;

/// <summary>
/// Works out what an app has from its files. For apps copied from the template before the state file existed:
/// a starting point the owner reviews, not a guarantee.
/// </summary>
public static class FeatureDetector
{
    private static readonly (string Id, string PathFragment)[] _platforms =
    [
        ("android", "/Platforms/Android/"),
        ("ios", "/Platforms/iOS/"),
        ("windows", "/Platforms/Windows/"),
        ("desktop", "/Platforms/Desktop/"),
        ("wasm", "/Platforms/WebAssembly/"),
    ];

    private static readonly (string Id, string PathFragment)[] _features =
    [
        ("http", "/Services/Http/"),
        ("dialogs", "/Services/Dialogs/"),
        ("launcher", "/Services/LauncherService.cs"),
        ("rating", "/Services/Rating/"),
        ("share", "/Services/ShareService.cs"),
        ("displayRequest", "/Services/DisplayRequestManager.cs"),
        ("czech", "/Strings/cs/"),
        ("devAssets", "/DevAssets.targets"),
        ("worktreeIdentity", "/WorktreeIdentity.props"),
        ("xamlStyler", "/Settings.XamlStyler"),
        ("tests", "/tests/"),
        ("ci", "/.github/workflows/ci.yml"),
        ("packaging", "/.github/workflows/tag-release.yml"),
        ("agents", "/AGENTS.md"),
        ("specKit", "/.specify/"),
        ("community", "/.github/ISSUE_TEMPLATE/"),
    ];

    public static Dictionary<string, string> Detect(IEnumerable<string> repositoryFiles, Func<string, string?> readFile)
    {
        var rooted = repositoryFiles.Select(file => "/" + file.Replace('\\', '/')).ToList();
        bool Has(string fragment) => rooted.Any(file => file.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        Dictionary<string, string> options = new(StringComparer.Ordinal)
        {
            ["platforms"] = string.Join('|', _platforms.Where(platform => Has(platform.PathFragment)).Select(platform => platform.Id)),
        };

        foreach (var (id, fragment) in _features)
        {
            options[id] = Has(fragment) ? "True" : "False";
        }

        var packages = rooted.FirstOrDefault(file => file.EndsWith("/Directory.Packages.props", StringComparison.OrdinalIgnoreCase));
        var hasSqlite = packages is not null && (readFile(packages[1..])?.Contains("sqlite", StringComparison.OrdinalIgnoreCase) ?? false);
        options["sqlite"] = hasSqlite ? "True" : "False";

        return options;
    }
}
