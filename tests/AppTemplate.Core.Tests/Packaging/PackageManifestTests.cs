using System.Xml.Linq;
using FluentAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public class PackageManifestTests
{
    private static readonly XNamespace _uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";

    [TestMethod]
    public void VisualElements_Always_HasTransparentBackgroundColor()
    {
        var manifest = XDocument.Load(Path.Combine(FindRepoRoot(), "src", "AppTemplate", "Package.appxmanifest"));

        var visualElements = manifest.Descendants(_uap + "VisualElements").Single();

        ((string?)visualElements.Attribute("BackgroundColor")).Should().Be("transparent");
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
