using AwesomeAssertions;

namespace Template.GenerationTests;

/// <summary>Compiles generated apps. Slow: each row is a full restore and build of the head.</summary>
[TestClass]
[TestCategory("Build")]
[DoNotParallelize]
public class BuildTests
{
    private const string _desktop = "net10.0-desktop";

    private static void Build(GeneratedApp app, string framework)
    {
        var project = app.Path($"src/{app.Name}/{app.Name}.csproj");
        TemplateFixture.Run("dotnet", ["build", project, "-f", framework, "-v", "q", "-nologo"], app.Root);
    }

    private static void GitInit(GeneratedApp app)
    {
        TemplateFixture.Run("git", ["init", "-q"], app.Root);
        TemplateFixture.Run("git", ["add", "-A"], app.Root);
        TemplateFixture.Run("git", ["-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-q", "-m", "Initial commit"], app.Root);
    }

    [TestMethod]
    public void Default_BuildsAndItsTestsPass()
    {
        var app = TemplateFixture.Generate("b-default", "Contoso");
        GitInit(app);

        Build(app, _desktop);

        var test = TemplateFixture.Run("dotnet", ["test", app.Path("tests/Contoso.Core.Tests/Contoso.Core.Tests.csproj")], app.Root);
        test.Should().Contain("succeeded:").And.NotContain("failed: 1");
    }

    [TestMethod]
    [DataRow("nopkg", new[] { "--platforms", "desktop", "--packaging", "false" })]
    [DataRow("desktop", new[] { "--platforms", "desktop" })]
    [DataRow("noci", new[] { "--platforms", "desktop", "--ci", "false", "--packaging", "false" })]
    public void WithoutThePipeline_TheAppsOwnTestsStillPass(string scenario, string[] options)
    {
        // Several tests read workflow files, which an app may not have.
        var app = TemplateFixture.Generate($"b-tests-{scenario}", "Contoso", options);
        GitInit(app);

        var test = TemplateFixture.Run("dotnet", ["test", app.Path("tests/Contoso.Core.Tests/Contoso.Core.Tests.csproj")], app.Root);

        test.Should().Contain("succeeded:").And.NotContain("failed: 1");
    }

    [TestMethod]
    public void AgentsAndAndroidOff_TheAppsOwnTestsStillPass()
    {
        // Tests that look for an optional file to find the root, or that get excluded with one platform's, show up here.
        var app = TemplateFixture.Generate("b-tests-noagents", "Contoso", "--platforms", "desktop", "--agents", "false", "--spec-kit", "false");
        GitInit(app);

        var test = TemplateFixture.Run("dotnet", ["test", app.Path("tests/Contoso.Core.Tests/Contoso.Core.Tests.csproj")], app.Root);

        test.Should().Contain("succeeded:").And.NotContain("failed: 1");
        app.Exists("tests/Contoso.Core.Tests/Packaging/PackageManifestTests.cs").Should().BeTrue();
        app.Exists("tests/Contoso.Core.Tests/Packaging/AndroidManifestTests.cs").Should().BeFalse();
    }

    [TestMethod]
    public void Default_BuildsWithoutAGitRepository()
    {
        // `dotnet new` alone doesn't create a repository, and that must not be a dead end.
        var app = TemplateFixture.Generate("b-nogit", "Contoso", "--platforms", "desktop");

        Build(app, _desktop);
    }

    [TestMethod]
    [DataRow("http")]
    [DataRow("sqlite")]
    [DataRow("share")]
    [DataRow("display-request")]
    [DataRow("rating")]
    [DataRow("czech")]
    [DataRow("dev-assets")]
    [DataRow("worktree-identity")]
    [DataRow("xaml-styler")]
    [DataRow("tests")]
    [DataRow("ci")]
    [DataRow("packaging")]
    [DataRow("spec-kit")]
    [DataRow("community")]
    public void FeatureOff_Builds(string feature)
    {
        var app = TemplateFixture.Generate($"b-{feature}", "Contoso", "--platforms", "desktop", $"--{feature}", "false");
        GitInit(app);

        Build(app, _desktop);
    }

    [TestMethod]
    public void EverythingOff_Builds()
    {
        var app = TemplateFixture.Generate(
            "b-minimal", "Contoso", "--platforms", "desktop",
            "--http", "false", "--sqlite", "false", "--dialogs", "false", "--launcher", "false", "--rating", "false",
            "--share", "false", "--display-request", "false", "--czech", "false", "--dev-assets", "false",
            "--worktree-identity", "false", "--xaml-styler", "false", "--tests", "false", "--ci", "false",
            "--packaging", "false", "--agents", "false", "--spec-kit", "false", "--community", "false");
        GitInit(app);

        Build(app, _desktop);
    }

    [TestMethod]
    public void DialogsOnlyWithoutRating_Builds()
    {
        var app = TemplateFixture.Generate("b-dialogs", "Contoso", "--platforms", "desktop", "--rating", "false", "--launcher", "false");
        GitInit(app);

        Build(app, _desktop);
    }

    [TestMethod]
    [DataRow("wasm", "net10.0-browserwasm")]
    [DataRow("windows", "net10.0-windows10.0.26100")]
    [DataRow("android", "net10.0-android")]
    [DataRow("tvos", "net10.0-tvos")]
    public void SinglePlatform_Builds(string platform, string framework)
    {
        var app = TemplateFixture.Generate($"b-only-{platform}", "Contoso", "--platforms", platform);
        GitInit(app);

        Build(app, framework);
    }

    [TestMethod]
    public void WorktreeIdentityOff_WindowsHeadBuilds()
    {
        var app = TemplateFixture.Generate("b-win-nowt", "Contoso", "--platforms", "windows", "--worktree-identity", "false", "--dev-assets", "false");
        GitInit(app);

        Build(app, "net10.0-windows10.0.26100");
    }
}
