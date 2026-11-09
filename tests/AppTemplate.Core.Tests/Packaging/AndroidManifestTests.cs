using System.Xml.Linq;
using AwesomeAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public class AndroidManifestTests
{
    private static readonly XNamespace _android = "http://schemas.android.com/apk/res/android";

    [TestMethod]
    public void Manifest_DeclaresInternetPermission()
    {
        // Debug builds add INTERNET on their own, so only a Release build would reveal it missing.
        var manifest = XDocument.Load(Path.Combine(FindRepoRoot(), "src", "AppTemplate", "Platforms", "Android", "AndroidManifest.xml"));

        var permissions = manifest.Root!.Elements("uses-permission").Select(e => (string?)e.Attribute(_android + "name"));

        permissions.Should().Contain("android.permission.INTERNET");
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "version.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root (version.json) not found.");
    }
}
