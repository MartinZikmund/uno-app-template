using AwesomeAssertions;
using MZikmund.UnoApp.Wizard;

namespace Template.SelfTests.Wizard;

[TestClass]
public class WizardSelectionTests
{
    private static WizardSelection NewSelection(string name = "Contoso") => new(WizardTestData.RealManifest()) { Name = name };

    [TestMethod]
    public void Defaults_EverythingOn_CommandStaysMinimal()
    {
        var selection = NewSelection();

        selection.ToCommandLine().Should().Be("dotnet new mzuno-app -n Contoso");
        selection.Validate().Should().BeEmpty();
    }

    [TestMethod]
    public void SetFeature_On_PullsInRequirements()
    {
        var selection = NewSelection();
        selection.SetFeature("rating", false);
        selection.SetFeature("dialogs", false);
        selection.SetFeature("launcher", false);

        selection.SetFeature("rating", true);

        selection.IsEnabled("dialogs").Should().BeTrue();
        selection.IsEnabled("launcher").Should().BeTrue();
    }

    [TestMethod]
    public void SetFeature_OffWhileRequired_IsRefused()
    {
        var selection = NewSelection();

        selection.SetFeature("dialogs", false).Should().BeFalse();

        selection.IsEnabled("dialogs").Should().BeTrue();
        selection.RequiredBy("dialogs").Should().ContainSingle().Which.Should().Be("rating");
    }

    [TestMethod]
    public void SetFeature_OffOnceNothingRequiresIt_Succeeds()
    {
        var selection = NewSelection();
        selection.SetFeature("rating", false);

        selection.SetFeature("dialogs", false).Should().BeTrue();

        selection.IsEnabled("dialogs").Should().BeFalse();
    }

    [TestMethod]
    public void SetFeature_Unknown_Throws()
    {
        var act = () => NewSelection().SetFeature("nope", true);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SetFeatures_DropsBothARequirementAndItsDependent()
    {
        var selection = NewSelection();
        var wanted = selection.Features.Except(["rating", "dialogs"]).ToList();

        var kept = selection.SetFeatures(wanted);

        kept.Should().BeEmpty();
        selection.IsEnabled("rating").Should().BeFalse();
        selection.IsEnabled("dialogs").Should().BeFalse();
    }

    [TestMethod]
    public void SetFeatures_KeepsAndReportsWhatsStillRequired()
    {
        var selection = NewSelection();
        var wanted = selection.Features.Except(["dialogs"]).ToList();

        var kept = selection.SetFeatures(wanted);

        kept.Should().ContainSingle().Which.Should().Be("dialogs");
        selection.IsEnabled("dialogs").Should().BeTrue();
    }

    [TestMethod]
    public void ToDotnetNewArguments_OnlyDifferencesFromDefaults()
    {
        var selection = NewSelection();
        selection.SetFeature("rating", false);
        selection.SetFeature("displayRequest", false);
        selection.SetPlatform("ios", false);
        selection.SetPlatform("android", false);
        selection.SetPlatform("wasm", false);
        selection.SetParameter("appId", "com.contoso.app");
        selection.SetParameter("title", "Contoso App");

        selection.ToCommandLine().Should().Be(
            "dotnet new mzuno-app -n Contoso --title \"Contoso App\" --app-id com.contoso.app " +
            "--platforms windows --platforms desktop --rating false --display-request false");
    }

    [TestMethod]
    public void ToDotnetNewArguments_ParameterEqualToDefault_IsOmitted()
    {
        var selection = NewSelection();
        selection.SetParameter("publisher", "Your Name");

        selection.ToDotnetNewArguments().Should().NotContain("--publisher");
    }

    [TestMethod]
    public void ToDotnetNewArguments_IncludesOutputDirectory()
    {
        var selection = NewSelection();
        selection.OutputDirectory = "D:/Apps/Contoso";

        selection.ToDotnetNewArguments().Should().ContainInOrder("-o", "D:/Apps/Contoso");
    }

    [TestMethod]
    [DataRow("", "Enter a project name")]
    [DataRow("1Bad", "must start with a letter")]
    [DataRow("My App", "must start with a letter")]
    [DataRow("../evil", "must start with a letter")]
    [DataRow("AppTemplate", "other than AppTemplate")]
    public void Validate_BadName_Reports(string name, string expected)
    {
        NewSelection(name).Validate().Should().Contain(problem => problem.Contains(expected));
    }

    [TestMethod]
    [DataRow("Contoso")]
    [DataRow("Contoso.Notes2")]
    public void Validate_GoodName_Passes(string name)
    {
        NewSelection(name).Validate().Should().BeEmpty();
    }

    [TestMethod]
    public void Validate_NoPlatforms_Reports()
    {
        var selection = NewSelection();
        foreach (var platform in selection.Platforms.ToList())
        {
            selection.SetPlatform(platform, false);
        }

        selection.Validate().Should().Contain(problem => problem.Contains("at least one platform"));
    }

    [TestMethod]
    [DataRow("Com.Contoso", false)]
    [DataRow("contoso", false)]
    [DataRow("com.1contoso", false)]
    [DataRow("com.contoso.app", true)]
    public void Validate_AppId(string appId, bool valid)
    {
        var selection = NewSelection();
        selection.SetParameter("appId", appId);

        selection.Validate().Any(problem => problem.Contains("application id")).Should().Be(!valid);
    }

    [TestMethod]
    public void Validate_LongTitle_Reports()
    {
        var selection = NewSelection();
        selection.SetParameter("title", new string('x', 37));

        selection.Validate().Should().Contain(problem => problem.Contains("display name"));
    }

    [TestMethod]
    [DataRow("Tom & Jerry")]
    [DataRow("A<B")]
    [DataRow("Say \"hi\"")]
    [DataRow("back\\slash")]
    [DataRow("-o=evil")]
    [DataRow("Tom's App")]
    [DataRow("$(Evil)")]
    [DataRow("semi;colon")]
    [DataRow("line\nbreak")]
    public void Validate_TitleOrPublisherThatBreaksGeneratedFiles_Reports(string value)
    {
        var title = NewSelection();
        title.SetParameter("title", value);
        var publisher = NewSelection();
        publisher.SetParameter("publisher", value);

        title.Validate().Should().Contain(problem => problem.Contains("display name"));
        publisher.Validate().Should().Contain(problem => problem.Contains("publisher"));
    }

    [TestMethod]
    [DataRow("Contoso Notes 2")]
    [DataRow("Contoso, Inc.")]
    [DataRow("Žluťoučký kůň")]
    [DataRow("Notes (Beta)")]
    public void Validate_OrdinaryTitleAndPublisher_Pass(string value)
    {
        var selection = NewSelection();
        selection.SetParameter("title", value);
        selection.SetParameter("publisher", value);

        selection.Validate().Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("MyApp\n")]
    public void Validate_TrailingNewlineInName_Reports(string name)
    {
        NewSelection(name).Validate().Should().NotBeEmpty();
    }

    [TestMethod]
    public void Validate_TrailingNewlineInAppIdOrRepo_Reports()
    {
        var selection = NewSelection();
        selection.SetParameter("appId", "com.contoso.app\n");
        selection.SetParameter("githubRepo", "contoso/app\n");

        selection.Validate().Should().HaveCount(2);
    }

    [TestMethod]
    public void Validate_LongNameWithNoTitle_Reports()
    {
        // The title falls back to the name, so the same limit applies to it.
        NewSelection(new string('x', 37)).Validate().Should().Contain(problem => problem.Contains("36"));
    }

    [TestMethod]
    public void Validate_LongNameWithShortTitle_Passes()
    {
        var selection = NewSelection(new string('x', 37));
        selection.SetParameter("title", "Short");

        selection.Validate().Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("0.1.5", "0.1.141-ga2a1a3779a", true)]
    [DataRow("0.2.0", "0.1.141-ga2a1a3779a", false)]
    [DataRow("0.1.141-ga2a1a3779a", "0.1.141-gffffffffff", false)]
    [DataRow("unknown", "0.1.1", false)]
    [DataRow("0.1.1", "unknown", false)]
    public void IsOlder_ComparesNumericVersions(string version, string other, bool older)
    {
        TemplateState.IsOlder(version, other).Should().Be(older);
    }

    [TestMethod]
    public void Validate_BadRepo_Reports()
    {
        var selection = NewSelection();
        selection.SetParameter("githubRepo", "not a repo");

        selection.Validate().Should().Contain(problem => problem.Contains("owner/repo"));
    }
}
