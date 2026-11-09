using System.Text.Json;

namespace MZikmund.UnoApp.Wizard;

/// <summary>
/// The wizard's read-only endpoints. The local server answers them, and a hosted preview has no server, so it serves
/// them as files instead. The UI asks for the same paths either way and learns from the answer which one it got.
/// </summary>
public static class PreviewSite
{
    // A static host picks the content type from the extension, and the JSON client rejects anything else.
    public const string ManifestPath = "api/manifest.json";
    public const string InfoPath = "api/info.json";

    /// <summary>Adds those files to a published copy of the UI, so it can run without a server.</summary>
    public static void Write(string webRoot, TemplateManifest manifest, string version)
    {
        if (!File.Exists(Path.Combine(webRoot, "index.html")))
        {
            throw new DirectoryNotFoundException($"{webRoot} has no index.html, so it isn't a published wizard UI.");
        }

        Directory.CreateDirectory(Path.Combine(webRoot, "api"));
        File.WriteAllText(Path.Combine(webRoot, ManifestPath), JsonSerializer.Serialize(manifest, WizardJsonContext.Default.TemplateManifest));
        File.WriteAllText(Path.Combine(webRoot, InfoPath), JsonSerializer.Serialize(new WizardInfo(version, "", CanCreate: false), WizardJsonContext.Default.WizardInfo));
    }
}
