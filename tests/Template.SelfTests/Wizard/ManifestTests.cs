using FluentAssertions;
using MZikmund.UnoApp.Wizard;

namespace Template.SelfTests.Wizard;

[TestClass]
public class ManifestTests
{
    [TestMethod]
    public void Parse_RealTemplateConfig_ExposesPlatformsAndFeatures()
    {
        var manifest = WizardTestData.RealManifest();

        manifest.ShortName.Should().Be("mzuno-app");
        manifest.Platforms.Select(p => p.Id).Should().BeEquivalentTo("android", "ios", "windows", "desktop", "wasm");
        manifest.FindFeature("rating")!.CliName.Should().Be("rating");
        manifest.FindFeature("displayRequest")!.CliName.Should().Be("display-request");
        manifest.FindFeature("display-request")!.Id.Should().Be("displayRequest");
        manifest.Parameters.Select(p => p.CliName).Should().BeEquivalentTo("title", "app-id", "publisher", "github-repo");
    }

    [TestMethod]
    public void Parse_FeatureMissingFromWizardJson_Throws()
    {
        var parse = () => TemplateManifest.Parse(
            """{ "shortName": "x", "symbols": { "platforms": { "type": "parameter", "datatype": "choice", "choices": [] }, "orphan": { "type": "parameter", "datatype": "bool", "defaultValue": "true", "displayName": "O", "description": "O" } } }""",
            """{ "symbolInfo": {} }""",
            """{ "groups": [], "requires": {} }""");

        parse.Should().Throw<InvalidDataException>().WithMessage("*orphan*");
    }

    [TestMethod]
    public void Parse_WizardJsonListsUnknownFeature_Throws()
    {
        var parse = () => TemplateManifest.Parse(
            """{ "shortName": "x", "symbols": { "platforms": { "type": "parameter", "datatype": "choice", "choices": [] } } }""",
            """{ "symbolInfo": {} }""",
            """{ "groups": [ { "id": "g", "title": "G", "description": "D", "icon": "x", "features": [ "ghost" ] } ], "requires": {} }""");

        parse.Should().Throw<InvalidDataException>().WithMessage("*ghost*");
    }

    [TestMethod]
    public void RealConfig_EveryFeatureHasACliNameAndAnIdeEntry()
    {
        var manifest = WizardTestData.RealManifest();
        var ideHost = WizardTestData.ReadConfig("ide.host.json");

        foreach (var feature in manifest.Features)
        {
            ideHost.Should().Contain($"\"{feature.Id}\"", "the Visual Studio dialog lists every feature");
            feature.CliName.Should().MatchRegex("^[a-z][a-z-]*$");
        }
    }

    [TestMethod]
    public void RealConfig_EveryRequirementIsAKnownFeature()
    {
        var manifest = WizardTestData.RealManifest();

        foreach (var (feature, requirements) in manifest.Requires)
        {
            manifest.FindFeature(feature).Should().NotBeNull();
            requirements.Should().OnlyContain(requirement => manifest.FindFeature(requirement) != null);
        }
    }

    [TestMethod]
    public void RealConfig_StateFileRecordsEveryFeature()
    {
        var manifest = WizardTestData.RealManifest();
        var state = File.ReadAllText(Path.Combine(WizardTestData.RepoRoot, "template", "overlay", ".template-state.json"));

        foreach (var feature in manifest.Features)
        {
            state.Should().Contain($"\"{feature.Id}\": \"__opt_{feature.Id}__\"");
        }
    }
}
