using AwesomeAssertions;
using MZikmund.UnoApp.Wizard;

namespace Template.SelfTests.Wizard;

[TestClass]
public class GateAndDetectorTests
{
    [TestMethod]
    [DataRow("src/AppTemplate/App.xaml.cs", true)]
    [DataRow("src/Directory.Build.props", true)]
    [DataRow(".github/workflows/ci.yml", true)]
    [DataRow(".claude/rules/testing.md", true)]
    [DataRow("tests/AppTemplate.Core.Tests/IoCTests.cs", true)]
    [DataRow("global.json", true)]
    [DataRow("template/overlay/.template-state.json", true)]
    [DataRow("docs/building.md", false)]
    [DataRow("docs/upgrades/2026-01-01-a.md", false)]
    [DataRow("tools/Wizard.Cli/Program.cs", false)]
    [DataRow("tests/Template.SelfTests/Foo.cs", false)]
    [DataRow(".template.config/wizard.json", false)]
    [DataRow(".template.config/template.json", true)]
    [DataRow("AGENTS.md", true)]
    [DataRow("CLAUDE.md", true)]
    [DataRow(".specify/memory/constitution.md", true)]
    [DataRow(".claude/skills/speckit-plan/SKILL.md", true)]
    [DataRow(".github/PULL_REQUEST_TEMPLATE.md", true)]
    [DataRow(".github/workflows/template-selftest.yml", false)]
    [DataRow(".github/workflows/template-wizard-preview.yml", false)]
    [DataRow("tools/Wizard.Ui/wrangler.jsonc", false)]
    [DataRow("README.md", false)]
    [DataRow(@"src\AppTemplate\App.xaml", true)]
    public void Ships_ClassifiesPaths(string path, bool ships)
    {
        UpgradeNoteGate.Ships(path).Should().Be(ships);
    }

    [TestMethod]
    public void Evaluate_NoShippingChange_Passes()
    {
        var verdict = UpgradeNoteGate.Evaluate(["tools/Wizard.Cli/Program.cs", "docs/building.md"], [], null);

        verdict.Passed.Should().BeTrue();
    }

    [TestMethod]
    public void Evaluate_ShippingChangeWithoutNote_Fails()
    {
        var verdict = UpgradeNoteGate.Evaluate(["src/AppTemplate/App.xaml.cs"], [], "A description");

        verdict.Passed.Should().BeFalse();
        verdict.ShippingFiles.Should().ContainSingle();
        verdict.Message.Should().Contain("docs/upgrades/");
    }

    [TestMethod]
    public void Evaluate_ShippingChangeWithNote_Passes()
    {
        var verdict = UpgradeNoteGate.Evaluate(["src/AppTemplate/App.xaml.cs", "docs/upgrades/2026-11-08-thing.md"], ["docs/upgrades/2026-11-08-thing.md"], null);

        verdict.Passed.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("docs/upgrades/README.md")]
    [DataRow("docs/upgrades/_TEMPLATE.md")]
    public void Evaluate_EditingOnlyTheIndexOrTemplate_DoesNotCount(string file)
    {
        var verdict = UpgradeNoteGate.Evaluate(["src/AppTemplate/App.xaml.cs", file], [file], null);

        verdict.Passed.Should().BeFalse();
    }

    [TestMethod]
    public void Evaluate_EditingAnExistingNote_DoesNotCount()
    {
        var verdict = UpgradeNoteGate.Evaluate(["src/AppTemplate/App.xaml.cs", "docs/upgrades/2026-01-01-old.md"], [], null);

        verdict.Passed.Should().BeFalse();
    }

    [TestMethod]
    public void Evaluate_WaiverWithReason_Passes()
    {
        var verdict = UpgradeNoteGate.Evaluate(["src/AppTemplate/App.xaml.cs"], [], "Summary\n\nUpgrade-Note: none - only reorders a comment\n");

        verdict.Passed.Should().BeTrue();
        verdict.Message.Should().Contain("only reorders a comment");
    }

    [TestMethod]
    [DataRow("Upgrade-Note: none")]
    [DataRow("Upgrade-Note: none - ")]
    [DataRow("Upgrade-Note: yes - whatever")]
    [DataRow("Upgrade-Note: none\n- [ ] Bug fix")]
    [DataRow("Upgrade-Note: none -\n- [ ] Bug fix")]
    public void Evaluate_WaiverWithoutReason_Fails(string body)
    {
        var verdict = UpgradeNoteGate.Evaluate(["src/AppTemplate/App.xaml.cs"], [], body);

        verdict.Passed.Should().BeFalse();
    }

    [TestMethod]
    public void Detect_FindsTheTvosHead()
    {
        var options = FeatureDetector.Detect(["src/Stopwatch/Platforms/tvOS/Main.tvOS.cs"], _ => null);

        options["platforms"].Should().Be("tvos");
    }

    [TestMethod]
    public void Detect_ReadsPlatformsAndFeaturesFromFiles()
    {
        string[] files =
        [
            "src/Stopwatch/Platforms/Android/Main.Android.cs",
            "src/Stopwatch/Platforms/Windows/README.md",
            "src/Stopwatch/Services/Dialogs/DialogService.cs",
            "src/Stopwatch/Services/LauncherService.cs",
            "src/Directory.Packages.props",
            "tests/Stopwatch.Tests/Stopwatch.Tests.csproj",
            "AGENTS.md",
        ];

        var options = FeatureDetector.Detect(files, path => path.EndsWith("Directory.Packages.props", StringComparison.Ordinal) ? "<PackageVersion Include=\"sqlite-net-e\" />" : null);

        options["platforms"].Should().Be("android|windows");
        options["dialogs"].Should().Be("True");
        options["launcher"].Should().Be("True");
        options["rating"].Should().Be("False");
        options["tests"].Should().Be("True");
        options["agents"].Should().Be("True");
        options["specKit"].Should().Be("False");
        options["sqlite"].Should().Be("True");
    }

    [TestMethod]
    [DataRow(".github/workflows/release.yml")]
    [DataRow(".github/workflows/tag-release.yml")]
    [DataRow(".github/workflows/package-windows.yml")]
    public void Detect_PackagingFromEitherGenerationOfTheWorkflows(string workflow)
    {
        // Apps copied before the multi-store pipeline have the older files.
        FeatureDetector.Detect([workflow], _ => null)["packaging"].Should().Be("True");
    }

    [TestMethod]
    public void Detect_NoReleaseWorkflow_HasNoPackaging()
    {
        FeatureDetector.Detect([".github/workflows/ci.yml"], _ => null)["packaging"].Should().Be("False");
    }

    [TestMethod]
    public void Detect_CoversEveryFeatureInTheManifest()
    {
        var options = FeatureDetector.Detect([], _ => null);

        foreach (var feature in WizardTestData.RealManifest().Features)
        {
            options.Should().ContainKey(feature.Id);
        }
    }
}
