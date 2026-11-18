using System.Buffers.Binary;
using System.Xml.Linq;
using AwesomeAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public class MacBundleTests
{
    [TestMethod]
    public void InfoPlist_HasAnAppStoreCategory()
    {
        var info = ReadPlist("Info.plist");

        info.Should().ContainKey("LSApplicationCategoryType")
            .WhoseValue.Value.Should().StartWith("public.app-category.", "the Mac App Store rejects a bundle without a category");
    }

    [TestMethod]
    public void InfoPlist_DeclaresNoNonExemptEncryption()
    {
        var info = ReadPlist("Info.plist");

        info.Should().ContainKey("ITSAppUsesNonExemptEncryption").WhoseValue.Name.LocalName.Should().Be("false");
    }

    [TestMethod]
    public void InfoPlist_ListsEveryAppLanguage()
    {
        var info = ReadPlist("Info.plist");
        var appLanguages = Directory.GetDirectories(Path.Combine(FindRepoRoot(), "src", "AppTemplate", "Strings")).Select(Path.GetFileName);

        var bundleLanguages = info["CFBundleLocalizations"].Elements("string").Select(e => e.Value);

        bundleLanguages.Should().BeEquivalentTo(appLanguages);
    }

    [TestMethod]
    public void Entitlements_AreSandboxed()
    {
        var entitlements = ReadPlist("Entitlements.plist");

        entitlements.Should().ContainKey("com.apple.security.app-sandbox").WhoseValue.Name.LocalName.Should().Be("true");
    }

    [TestMethod]
    [DataRow("com.apple.security.cs.allow-jit", "the .NET runtime compiles code at run time")]
    //#if (http)
    [DataRow("com.apple.security.network.client", "the HTTP client makes outgoing connections")]
    //#endif
    public void Entitlements_AllowWhatTheAppUses(string key, string because)
    {
        var entitlements = ReadPlist("Entitlements.plist");

        entitlements.Should().ContainKey(key, because).WhoseValue.Name.LocalName.Should().Be("true");
    }

    [TestMethod]
    public void AppIcon_HasEverySizeTheIcnsNeeds()
    {
        var folder = Path.Combine(MacOSFolder(), "AppIcon");

        foreach (var points in new[] { 16, 32, 128, 256, 512 })
        {
            foreach (var scale in new[] { 1, 2 })
            {
                var file = Path.Combine(folder, $"icon{points}x{points}@{scale}x.png");
                File.Exists(file).Should().BeTrue($"Uno builds icon.icns only from images with these names, and {Path.GetFileName(file)} is missing");
                PngSize(file).Should().Be((points * scale, points * scale));
            }
        }
    }

    private static (int Width, int Height) PngSize(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = File.OpenRead(path);
        stream.ReadExactly(header);
        return (BinaryPrimitives.ReadInt32BigEndian(header[16..]), BinaryPrimitives.ReadInt32BigEndian(header[20..]));
    }

    private static Dictionary<string, XElement> ReadPlist(string name)
    {
        var dict = XDocument.Load(Path.Combine(MacOSFolder(), name)).Root!.Element("dict")!;
        return dict.Elements("key").ToDictionary(k => k.Value, k => (XElement)k.NextNode!);
    }

    private static string MacOSFolder() => Path.Combine(FindRepoRoot(), "src", "AppTemplate", "Platforms", "Desktop", "macOS");

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
