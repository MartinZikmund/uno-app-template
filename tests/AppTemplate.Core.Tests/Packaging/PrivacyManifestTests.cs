using System.Xml.Linq;
using AwesomeAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public class PrivacyManifestTests
{
    [TestMethod]
    public void PrivacyManifests_AreOnePlistDictionaryWithTheirApiReasonsInAnArray()
    {
        // App Store Connect rejects a manifest that isn't a valid plist, and the build doesn't check it.
        var manifests = Directory.GetFiles(Path.Combine(FindRepoRoot(), "src", "AppTemplate", "Platforms"), "PrivacyInfo.xcprivacy", SearchOption.AllDirectories);
        manifests.Should().NotBeEmpty();

        foreach (var path in manifests)
        {
            var root = XDocument.Load(path).Root!;
            root.Elements().Select(e => e.Name.LocalName).Should().Equal(["dict"], path);

            var dict = root.Element("dict")!;
            ValueAfter(dict, "NSPrivacyTracking").Should().NotBeNull(path);
            ValueAfter(dict, "NSPrivacyTracking")!.Name.LocalName.Should().Be("false", path);

            var apis = ValueAfter(dict, "NSPrivacyAccessedAPITypes");
            apis.Should().NotBeNull(path);
            apis!.Name.LocalName.Should().Be("array", path);
            apis.Elements("dict").Should().NotBeEmpty(path).And.AllSatisfy(api =>
            {
                ValueAfter(api, "NSPrivacyAccessedAPIType")!.Value.Should().StartWith("NSPrivacyAccessedAPICategory", path);
                ValueAfter(api, "NSPrivacyAccessedAPITypeReasons")!.Elements("string").Should().NotBeEmpty(path);
            });
        }
    }

    private static XElement? ValueAfter(XElement dict, string key) =>
        dict.Elements("key").FirstOrDefault(k => k.Value == key)?.ElementsAfterSelf().FirstOrDefault();

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
