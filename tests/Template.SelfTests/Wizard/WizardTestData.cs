using MZikmund.UnoApp.Wizard;

namespace Template.SelfTests.Wizard;

internal static class WizardTestData
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string ReadConfig(string file) => File.ReadAllText(Path.Combine(RepoRoot, ".template.config", file));

    public static TemplateManifest RealManifest() =>
        TemplateManifest.Parse(ReadConfig("template.json"), ReadConfig("dotnetcli.host.json"), ReadConfig("wizard.json"));

    public static TemplateState State(string platforms = "android|windows", string[]? on = null, string[]? applied = null)
    {
        Dictionary<string, string> options = new() { ["platforms"] = platforms };
        foreach (var feature in RealManifest().Features)
        {
            options[feature.Id] = (on is null || on.Contains(feature.Id)) ? "True" : "False";
        }

        return new TemplateState(TemplateState.TemplatePackageId, "0.1.5", null, "Contoso", options, applied ?? []);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, ".template.config", "template.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Template root not found.");
    }
}
