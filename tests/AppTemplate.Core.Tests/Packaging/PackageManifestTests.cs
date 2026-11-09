using System.Xml.Linq;
using AwesomeAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public class PackageManifestTests
{
    private static readonly XNamespace _foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
    private static readonly XNamespace _uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";

    [TestMethod]
    public void VisualElements_Always_HasTransparentBackgroundColor()
    {
        var manifest = XDocument.Load(Path.Combine(FindRepoRoot(), "src", "AppTemplate", "Package.appxmanifest"));

        var visualElements = manifest.Descendants(_uap + "VisualElements").Single();

        ((string?)visualElements.Attribute("BackgroundColor")).Should().Be("transparent");
    }

    [TestMethod]
    public void Identity_Always_HasPlaceholderVersion()
    {
        var manifest = XDocument.Load(Path.Combine(FindRepoRoot(), "src", "AppTemplate", "Package.appxmanifest"));

        var identity = manifest.Descendants(_foundation + "Identity").Single();

        ((string?)identity.Attribute("Version")).Should().Be("0.0.0.0", "Nerdbank.GitVersioning stamps the real version at build time");
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root (AGENTS.md) not found.");
    }
}
