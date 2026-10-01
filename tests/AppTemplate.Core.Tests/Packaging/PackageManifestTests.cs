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
        var manifest = XDocument.Load(FindRepoFile("src", "AppTemplate", "Package.appxmanifest"));

        var visualElements = manifest.Descendants(_uap + "VisualElements").Single();

        ((string?)visualElements.Attribute("BackgroundColor")).Should().Be("transparent");
    }

    private static string FindRepoFile(params string[] segments)
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, .. segments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Could not find {Path.Combine(segments)} above {AppContext.BaseDirectory}.");
    }
}
