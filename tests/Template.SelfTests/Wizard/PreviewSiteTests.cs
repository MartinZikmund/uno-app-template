using System.Text.Json;
using AwesomeAssertions;
using MZikmund.UnoApp.Wizard;

namespace Template.SelfTests.Wizard;

[TestClass]
public class PreviewSiteTests
{
    private string _webRoot = "";

    [TestInitialize]
    public void CreateWebRoot()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), "mzpreview" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<html></html>");
    }

    [TestCleanup]
    public void DeleteWebRoot() => Directory.Delete(_webRoot, recursive: true);

    [TestMethod]
    public void Write_Info_CannotCreateAndNamesNoFolder()
    {
        PreviewSite.Write(_webRoot, WizardTestData.RealManifest(), "1.2.3");

        var info = JsonSerializer.Deserialize(ReadWritten(PreviewSite.InfoPath), WizardJsonContext.Default.WizardInfo)!;

        info.CanCreate.Should().BeFalse();
        info.OutputRoot.Should().BeEmpty();
        info.Version.Should().Be("1.2.3");
    }

    [TestMethod]
    public void Write_Manifest_IsTheTemplatesOwn()
    {
        var expected = WizardTestData.RealManifest();

        PreviewSite.Write(_webRoot, expected, "1.2.3");

        var manifest = JsonSerializer.Deserialize(ReadWritten(PreviewSite.ManifestPath), WizardJsonContext.Default.TemplateManifest)!;
        manifest.ShortName.Should().Be(expected.ShortName);
        manifest.Platforms.Select(p => p.Id).Should().Equal(expected.Platforms.Select(p => p.Id));
        manifest.Features.Select(f => f.Id).Should().Equal(expected.Features.Select(f => f.Id));
        manifest.Requires["rating"].Should().BeEquivalentTo(expected.Requires["rating"]);
    }

    [TestMethod]
    public void Write_PathsAreJson_SoBrowsersGetAJsonContentType()
    {
        PreviewSite.ManifestPath.Should().EndWith(".json");
        PreviewSite.InfoPath.Should().EndWith(".json");
    }

    [TestMethod]
    public void Write_NotAPublishedSite_Fails()
    {
        File.Delete(Path.Combine(_webRoot, "index.html"));

        var write = () => PreviewSite.Write(_webRoot, WizardTestData.RealManifest(), "1.2.3");

        write.Should().Throw<DirectoryNotFoundException>().WithMessage("*index.html*");
    }

    [TestMethod]
    public void LocalServerInfo_CanCreate() => new WizardInfo("1.2.3", "/apps").CanCreate.Should().BeTrue();

    [TestMethod]
    public void Command_WithThePreviewsEmptyOutputRoot_HasNoOutputOption()
    {
        WizardSelection selection = new(WizardTestData.RealManifest()) { Name = "Contoso", OutputDirectory = "" };

        selection.Validate().Should().BeEmpty();
        selection.ToDotnetNewArguments().Should().NotContain("-o");
        selection.ToCommandLine().Should().StartWith("dotnet new mzuno-app -n Contoso");
    }

    private string ReadWritten(string relative) => File.ReadAllText(Path.Combine(_webRoot, relative));
}
