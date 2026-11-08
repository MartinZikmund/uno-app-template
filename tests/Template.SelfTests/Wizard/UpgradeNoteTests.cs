using FluentAssertions;
using MZikmund.UnoApp.Wizard;

namespace Template.SelfTests.Wizard;

[TestClass]
public class UpgradeNoteTests
{
    private const string _validBody = """

        ## Why
        w

        ## Who is affected
        a

        ## Steps
        s

        ## Verify
        v
        """;

    private static string Note(string frontMatter) => $"---\n{frontMatter}\n---\n{_validBody}";

    private static UpgradeNote Parsed(string id, string features = "", string platforms = "") =>
        UpgradeNote.Parse(id, Note($"title: T\nimpact: recommended\nfeatures: [{features}]\nplatforms: [{platforms}]"));

    [TestMethod]
    public void Parse_ValidNote_ReadsFrontMatter()
    {
        var note = UpgradeNote.Parse("2026-11-08-some-change", Note("title: \"Some change\"\nimpact: breaking\nfeatures: [rating, share]\nplatforms: [android]"));

        note.Id.Should().Be("2026-11-08-some-change");
        note.Title.Should().Be("Some change");
        note.Impact.Should().Be(UpgradeImpact.Breaking);
        note.Features.Should().Equal("rating", "share");
        note.Platforms.Should().Equal("android");
        note.Body.Should().StartWith("## Why");
    }

    [TestMethod]
    public void Parse_HandlesCrLf()
    {
        var note = UpgradeNote.Parse("2026-11-08-crlf", Note("title: T\nimpact: optional").Replace("\n", "\r\n"));

        note.Impact.Should().Be(UpgradeImpact.Optional);
    }

    [TestMethod]
    [DataRow("no-front-matter", "not a note", "front matter")]
    [DataRow("2026-11-08-a", "---\nimpact: optional\n---\n", "title")]
    [DataRow("2026-11-08-a", "---\ntitle: T\n---\n", "impact")]
    [DataRow("2026-11-08-a", "---\ntitle: T\nimpact: sometimes\n---\n", "impact")]
    [DataRow("2026-11-08-a", "---\ntitle: T\nimpact: optional\n---\n## Why\nw\n", "Steps")]
    public void Parse_Invalid_ExplainsWhat(string id, string markdown, string expected)
    {
        var parse = () => UpgradeNote.Parse(id, markdown);

        parse.Should().Throw<InvalidDataException>().WithMessage($"*{expected}*");
    }

    [TestMethod]
    [DataRow("Some_Name")]
    [DataRow("26-11-08-short-year")]
    public void Parse_BadFileName_Throws(string id)
    {
        var parse = () => UpgradeNote.Parse(id, Note("title: T\nimpact: optional"));

        parse.Should().Throw<InvalidDataException>().WithMessage("*YYYY-MM-DD*");
    }

    [TestMethod]
    public void Pending_FiltersAppliedAndSortsOldestFirst()
    {
        var state = WizardTestData.State(applied: ["2026-01-02-b"]);
        UpgradeNote[] catalog = [Parsed("2026-03-01-c"), Parsed("2026-01-02-b"), Parsed("2026-01-01-a")];

        UpgradePlanner.Pending(catalog, state).Select(n => n.Id).Should().Equal("2026-01-01-a", "2026-03-01-c");
    }

    [TestMethod]
    public void Pending_AppliedOutOfOrder_StillFindsEarlierNote()
    {
        // A note that merged late but is dated early must not be skipped.
        var state = WizardTestData.State(applied: ["2026-06-01-later"]);

        UpgradePlanner.Pending([Parsed("2026-01-01-earlier")], state).Should().ContainSingle();
    }

    [TestMethod]
    public void Pending_SkipsFeaturesTheAppDoesNotHave()
    {
        var state = WizardTestData.State(on: ["share"]);
        UpgradeNote[] catalog = [Parsed("2026-01-01-rating", features: "rating"), Parsed("2026-01-02-share", features: "share"), Parsed("2026-01-03-all")];

        UpgradePlanner.Pending(catalog, state).Select(n => n.Id).Should().Equal("2026-01-02-share", "2026-01-03-all");
    }

    [TestMethod]
    public void Pending_SkipsPlatformsTheAppDoesNotHave()
    {
        var state = WizardTestData.State(platforms: "windows");
        UpgradeNote[] catalog = [Parsed("2026-01-01-android", platforms: "android"), Parsed("2026-01-02-windows", platforms: "windows, ios")];

        UpgradePlanner.Pending(catalog, state).Select(n => n.Id).Should().Equal("2026-01-02-windows");
    }

    [TestMethod]
    public void State_RoundTrips()
    {
        var state = WizardTestData.State(applied: ["2026-01-01-a"]);

        var parsed = TemplateState.Parse(state.ToJson());

        parsed.Name.Should().Be("Contoso");
        parsed.HasOption("rating").Should().BeTrue();
        parsed.Platforms.Should().BeEquivalentTo("android", "windows");
        parsed.AppliedUpgrades.Should().Equal("2026-01-01-a");
    }

    [TestMethod]
    public void State_MarkApplied_AddsWithoutDuplicates()
    {
        var state = WizardTestData.State(applied: ["2026-01-02-b"]);

        var updated = state.MarkApplied(["2026-01-01-a", "2026-01-02-b"]);

        updated.AppliedUpgrades.Should().Equal("2026-01-01-a", "2026-01-02-b");
    }

    [TestMethod]
    public void State_ToDotnetNewOptionArguments_RegeneratesChoices()
    {
        var state = WizardTestData.State(platforms: "windows|desktop", on: ["share"]);

        var arguments = state.ToDotnetNewOptionArguments(WizardTestData.RealManifest());

        string.Join(' ', arguments).Should().Contain("--platforms desktop --platforms windows")
            .And.Contain("--rating false")
            .And.Contain("--share true");
    }

    [TestMethod]
    public void State_ToDotnetNewOptionArguments_SkipsOptionsFromAnotherVersion()
    {
        var state = WizardTestData.State() with { Options = new Dictionary<string, string> { ["retiredFeature"] = "True" } };

        state.ToDotnetNewOptionArguments(WizardTestData.RealManifest()).Should().BeEmpty();
    }

    [TestMethod]
    public void RealNotes_AllParseAndReferenceRealFeaturesAndPlatforms()
    {
        var manifest = WizardTestData.RealManifest();
        var directory = Path.Combine(WizardTestData.RepoRoot, "docs", "upgrades");

        var files = Directory.GetFiles(directory, "*.md").Where(f => Path.GetFileName(f) is not ("README.md" or "_TEMPLATE.md")).ToList();
        files.Should().NotBeEmpty();
        foreach (var file in files)
        {
            var note = UpgradeNote.Parse(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));

            note.Features.Should().OnlyContain(feature => manifest.Features.Any(f => f.Id == feature), $"{note.Id} must use feature ids from wizard.json");
            note.Platforms.Should().OnlyContain(platform => manifest.Platforms.Any(p => p.Id == platform), $"{note.Id} must use platform ids");
        }
    }
}
