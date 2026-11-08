using System.Reflection;
using MZikmund.UnoApp.Wizard;

namespace MZikmund.UnoApp.Cli;

/// <summary>What ships inside the tool package.</summary>
internal static class Bundled
{
    private static readonly Assembly _assembly = typeof(Bundled).Assembly;

    public static string Version =>
        _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";

    public static string UiRoot => Path.Combine(AppContext.BaseDirectory, "ui");

    public static TemplateManifest Manifest() =>
        TemplateManifest.Parse(Read("template.json"), Read("dotnetcli.host.json"), Read("wizard.json"));

    public static IReadOnlyList<UpgradeNote> UpgradeNotes() =>
        [.. _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("upgrades/", StringComparison.Ordinal))
            .Select(name => UpgradeNote.Parse(Path.GetFileNameWithoutExtension(name), Read(name)))];

    /// <summary>The template this tool was built with, or null when running from source without one.</summary>
    public static string? TemplatePackage()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "template");
        return Directory.Exists(directory) ? Directory.GetFiles(directory, "*.nupkg").FirstOrDefault() : null;
    }

    private static string Read(string name)
    {
        using var stream = _assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Missing resource {name}.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
