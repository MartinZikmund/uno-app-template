using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace Template.GenerationTests;

[TestClass]
public class OutputTests
{
    private static readonly string[] _leakedTokens =
    [
        "AppTemplate", "apptemplate", "App Template", "AppTmpl", "Martin Zikmund", "mzikmund",
        "__opt_", "__TEMPLATE_VERSION__", "__APPLIED_UPGRADES__",
        "Template.SelfTests", "template-selftest", "verify-dev-assets", "verify-worktree", "superpowers/",
    ];

    private static readonly Regex _marker = new(@"(//|<!--\s*|^\s*)#(if \(|else\b|endif\b)", RegexOptions.Multiline);

    [TestMethod]
    public void Default_ContainsEveryPlatformAndFeature()
    {
        var app = TemplateFixture.Generate("default-output", "ContosoNotes");

        app.Exists("src/ContosoNotes.slnx").Should().BeTrue();
        app.Exists("src/ContosoNotes/ContosoNotes.csproj").Should().BeTrue();
        app.Exists("src/ContosoNotes.Core/ContosoNotes.Core.csproj").Should().BeTrue();
        app.Exists("tests/ContosoNotes.Core.Tests/ContosoNotes.Core.Tests.csproj").Should().BeTrue();
        app.Exists("src/ContosoNotes/Services/Rating/AppRatingService.cs").Should().BeTrue();
        app.Exists("src/ContosoNotes/Platforms/Android/AndroidManifest.xml").Should().BeTrue();
        app.Exists("src/WorktreeIdentity.props").Should().BeTrue();
        app.Exists("AGENTS.md").Should().BeTrue();
        app.Exists(".specify").Should().BeTrue();
        app.Read("src/ContosoNotes/ContosoNotes.csproj").Should().Contain("net10.0-android").And.Contain("net10.0-browserwasm");
    }

    [TestMethod]
    public void Default_LeavesNoTemplateInternalsBehind()
    {
        var app = TemplateFixture.Generate("default-internals", "ContosoNotes");

        foreach (var path in new[]
        {
            ".template.config", "template", "tools", "scripts", "docs/superpowers", "docs/upgrades", "tests/Template.SelfTests",
            "tests/Template.GenerationTests", ".github/workflows/template-selftest.yml", ".github/FUNDING.yml",
            "docs/versioning-migration.md", ".claude/rules/template-upgrades.md", "LICENSE",
        })
        {
            app.Exists(path).Should().BeFalse($"{path} belongs to the template, not to an app");
        }

        app.Exists("docs/TEMPLATE-LICENSE.txt").Should().BeTrue();
        app.Read("docs/TEMPLATE-LICENSE.txt").Should().Contain("Martin Zikmund", "the MIT notice must survive");
    }

    [TestMethod]
    public void Default_LeaksNoTemplateIdentity()
    {
        var app = TemplateFixture.Generate("default-leaks", "ContosoNotes");

        List<string> leaks = [];
        foreach (var file in app.TextFiles())
        {
            if (file == "docs/TEMPLATE-LICENSE.txt")
            {
                continue;
            }

            var text = app.Read(file);
            leaks.AddRange(_leakedTokens.Where(text.Contains).Select(token => $"{file}: {token}"));
        }

        leaks.Should().BeEmpty();
    }

    [TestMethod]
    public void Default_KeepsTheToolPackageNameInUpgradeDocs()
    {
        var app = TemplateFixture.Generate("default-pkgname", "ContosoNotes");

        // sourceName must not rewrite references back to the template's own packages.
        app.Read("docs/template-upgrades.md").Should().Contain("MZikmund.UnoApp.Cli");
        app.Read(".template-state.json").Should().Contain("MZikmund.UnoApp.Templates");
    }

    [TestMethod]
    public void Pack_ContainsEveryTrackedTemplateFile()
    {
        var app = TemplateFixture.Generate("default-complete", "ContosoNotes");

        // Extensionless files are the known casualty: NuGet reads such a PackagePath as a folder and nests the file.
        app.AllFiles().Should().NotContain(f => Regex.IsMatch(f, @"(?:^|/)([^/.]+)/(?:.*/)?\1$"));
        File.Exists(app.Path("src/ContosoNotes/Platforms/WebAssembly/wwwroot/_headers")).Should().BeTrue();
        app.Exists(".specify/extensions/.registry").Should().BeTrue();
        app.Exists(".gitignore").Should().BeTrue();
        app.Exists(".editorconfig").Should().BeTrue();
        app.Exists(".config/dotnet-tools.json").Should().BeTrue();
    }

    [TestMethod]
    public void Default_LeavesNoConditionalMarkers()
    {
        var app = TemplateFixture.Generate("default-markers", "ContosoNotes");

        var offenders = app.TextFiles()
            .Where(f => Path.GetExtension(f) is not ".cs")
            .Where(f => _marker.IsMatch(app.Read(f)))
            .ToList();
        var csOffenders = app.AllFiles()
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal))
            .Where(f => app.Read(f).Contains("//#", StringComparison.Ordinal))
            .ToList();

        offenders.Should().BeEmpty();
        csOffenders.Should().BeEmpty();
    }

    [TestMethod]
    public void Default_KeepsRealPreprocessorDirectives()
    {
        var app = TemplateFixture.Generate("default-directives", "ContosoNotes");

        var appCode = app.Read("src/ContosoNotes/App.xaml.cs");
        appCode.Should().Contain("#if DEBUG").And.Contain("UseEnvironment(Environments.Development)");
        app.Read("src/ContosoNotes/WindowShell.xaml.cs").Should().Contain("#if !HAS_UNO");
        app.Read("src/ContosoNotes/Infrastructure/AppEnvironment.cs").Should().Contain("#if APP_CHANNEL_DEV && APP_CI_BUILD");
    }

    [TestMethod]
    public void Options_ApplyIdentity()
    {
        var app = TemplateFixture.Generate(
            "identity", "ContosoNotes", "--title", "Contoso Notes", "--app-id", "com.contoso.notes", "--publisher", "Contoso Ltd", "--github-repo", "contoso/notes");

        var csproj = app.Read("src/ContosoNotes/ContosoNotes.csproj");
        csproj.Should().Contain("<ApplicationTitle>Contoso Notes</ApplicationTitle>")
            .And.Contain("<ApplicationTitle>Contoso Notes Dev</ApplicationTitle>")
            .And.Contain("<ApplicationId>com.contoso.notes</ApplicationId>")
            .And.Contain("<ApplicationId>com.contoso.notes.dev</ApplicationId>")
            .And.Contain("<ApplicationPublisher>Contoso Ltd</ApplicationPublisher>");
        app.Read(".github/ISSUE_TEMPLATE/config.yml").Should().Contain("contoso/notes/discussions");
        app.Read(".template-state.json").Should().Contain("MartinZikmund/uno-app-template/tree/main/docs/upgrades", "upgrade notes stay in the template's repo");
        app.Read("docs/release-pipeline.md").Should().Contain("com.contoso.notes");
    }

    [TestMethod]
    public void Options_DefaultIdentityDerivesFromName()
    {
        var app = TemplateFixture.Generate("identity-default", "Contoso.Notes2");

        var csproj = app.Read("src/Contoso.Notes2/Contoso.Notes2.csproj");
        csproj.Should().Contain("<ApplicationId>com.example.contosonotes2</ApplicationId>")
            .And.Contain("<ApplicationTitle>Contoso.Notes2</ApplicationTitle>");
        app.Read("src/MobileLabels.targets").Should().Contain(">Contoso<", "the iOS short name is the first seven characters");
    }

    [TestMethod]
    public void StateFile_RecordsChoices()
    {
        var app = TemplateFixture.Generate("state", "ContosoNotes", "--platforms", "windows", "--platforms", "wasm", "--rating", "false");

        using var state = JsonDocument.Parse(app.Read(".template-state.json"));
        var root = state.RootElement;
        root.GetProperty("name").GetString().Should().Be("ContosoNotes");
        root.GetProperty("templateVersion").GetString().Should().MatchRegex(@"^\d+\.\d+");
        var options = root.GetProperty("options");
        options.GetProperty("rating").GetString().Should().BeEquivalentTo("false");
        options.GetProperty("share").GetString().Should().BeEquivalentTo("true");
        options.GetProperty("platforms").GetString()!.Split('|').Should().BeEquivalentTo("windows", "wasm");
        root.GetProperty("appliedUpgrades").EnumerateArray().Select(e => e.GetString()).Should().NotContain("__APPLIED_UPGRADES__");
        options.GetProperty("title").GetString().Should().Be("ContosoNotes");
        options.GetProperty("appId").GetString().Should().Be("com.example.contosonotes");
        options.GetProperty("publisher").GetString().Should().Be("Your Name");
        options.GetProperty("githubRepo").GetString().Should().Be("your-org/your-repo");
    }

    [TestMethod]
    [DataRow("http", "src/App1/Services/Http/DebugHttpHandler.cs", "UseHttp", "src/App1/App.xaml.cs")]
    [DataRow("sqlite", null, "sqlite-net-e", "src/Directory.Packages.props")]
    [DataRow("dialogs", null, null, null)]
    [DataRow("launcher", null, null, null)]
    [DataRow("rating", "src/App1/Services/Rating/AppRatingService.cs", "OfferUserRating", "src/App1/Services/Settings/AppPreferences.cs")]
    [DataRow("share", "src/App1/Services/ShareService.cs", "IShareService", "src/App1/App.xaml.cs")]
    [DataRow("display-request", "src/App1/Services/DisplayRequestManager.cs", "IDisplayRequestManager", "src/App1/App.xaml.cs")]
    [DataRow("czech", "src/App1/Strings/cs/Resources.resw", "\"cs\"", "src/App1/appsettings.json")]
    [DataRow("dev-assets", "src/DevAssets.targets", "DevAssets.targets", "src/Directory.Build.targets")]
    [DataRow("worktree-identity", "src/WorktreeIdentity.targets", "WorktreeName", "src/App1/WindowShell.xaml.cs")]
    [DataRow("xaml-styler", "Settings.XamlStyler", null, null)]
    [DataRow("tests", "tests/App1.Core.Tests/App1.Core.Tests.csproj", "Core.Tests", "src/App1.slnx")]
    [DataRow("ci", ".github/workflows/ci.yml", null, null)]
    [DataRow("packaging", ".github/workflows/release.yml", null, null)]
    [DataRow("agents", null, null, null)]
    [DataRow("spec-kit", ".specify/memory/constitution.md", null, null)]
    [DataRow("community", ".github/PULL_REQUEST_TEMPLATE.md", null, null)]
    public void FeatureOff_RemovesItsTraces(string option, string? removedFile, string? removedText, string? textFile)
    {
        var app = TemplateFixture.Generate($"off-{option}", "App1", $"--{option}", "false");

        if (removedFile is not null)
        {
            app.Exists(removedFile).Should().BeFalse();
        }

        if (removedText is not null)
        {
            app.Read(textFile!).Should().NotContain(removedText);
        }
    }

    [TestMethod]
    public void CzechOff_RemovesItFromTheIosLocalizationList()
    {
        var app = TemplateFixture.Generate("czech-plist", "App1", "--czech", "false");

        var plist = app.Read("src/App1/Platforms/iOS/Info.plist");
        plist.Should().Contain("<string>en</string>").And.NotContain("<string>cs</string>").And.NotContain("#if");
    }

    [TestMethod]
    public void IdeFiles_ListOnlyThePlatformsTheAppHas()
    {
        var app = TemplateFixture.Generate("ide-files", "App1", "--platforms", "windows");

        app.Read("src/.vscode/launch.json").Should().NotContain("net10.0-desktop").And.NotContain("browserwasm").And.NotContain("android");
        app.Read("src/.vscode/tasks.json").Should().NotContain("build-wasm").And.NotContain("build-desktop");
        app.Read("src/.run/App1.run.xml").Should().Contain("WinAppSDK").And.NotContain("Desktop").And.NotContain("WebAssembly");
    }

    [TestMethod]
    public void RatingOn_KeepsDialogsAndLauncherEvenWhenTurnedOff()
    {
        var app = TemplateFixture.Generate("rating-deps", "App1", "--dialogs", "false", "--launcher", "false");

        app.Exists("src/App1/Services/Dialogs/DialogService.cs").Should().BeTrue();
        app.Exists("src/App1/Services/LauncherService.cs").Should().BeTrue();
    }

    [TestMethod]
    public void RatingOff_DropsDialogsAndLauncherWhenTurnedOff()
    {
        var app = TemplateFixture.Generate("rating-nodeps", "App1", "--rating", "false", "--dialogs", "false", "--launcher", "false");

        app.Exists("src/App1/Services/Dialogs").Should().BeFalse();
        app.Exists("src/App1/Services/LauncherService.cs").Should().BeFalse();
        app.Read("src/App1/Strings/en/Resources.resw").Should().NotContain("RateAppTitle").And.NotContain("name=\"Yes\"");
    }

    [TestMethod]
    public void SpecKitOn_KeepsAgentsEvenWhenTurnedOff()
    {
        var app = TemplateFixture.Generate("spec-deps", "App1", "--agents", "false");

        app.Exists("AGENTS.md").Should().BeTrue();
        app.Exists(".claude/skills/speckit-plan/SKILL.md").Should().BeTrue();
    }

    [TestMethod]
    public void AgentsAndSpecKitOff_RemovesAllAgentFiles()
    {
        var app = TemplateFixture.Generate("no-agents", "App1", "--agents", "false", "--spec-kit", "false");

        app.Exists("AGENTS.md").Should().BeFalse();
        app.Exists("CLAUDE.md").Should().BeFalse();
        app.Exists(".claude").Should().BeFalse();
        app.Read("src/App1.slnx").Should().NotContain("CLAUDE.md");
    }

    [TestMethod]
    [DataRow("android", "Platforms/Android", "net10.0-android", ".github/workflows/_build-android.yml")]
    [DataRow("ios", "Platforms/iOS", "net10.0-ios", ".github/workflows/_build-ios.yml")]
    [DataRow("windows", "Platforms/Windows", "net10.0-windows", ".github/workflows/_build-windows.yml")]
    [DataRow("desktop", "Platforms/Desktop", "net10.0-desktop", ".github/workflows/_build-desktop.yml")]
    [DataRow("wasm", "Platforms/WebAssembly", "net10.0-browserwasm", ".github/workflows/_build-wasm.yml")]
    public void PlatformOff_RemovesItsHead(string platform, string folder, string tfm, string? workflow)
    {
        string[] all = ["android", "ios", "windows", "desktop", "wasm"];
        var options = all.Where(p => p != platform).SelectMany(p => new[] { "--platforms", p }).ToArray();

        var app = TemplateFixture.Generate($"no-{platform}", "App1", options);

        app.Exists($"src/App1/{folder}").Should().BeFalse();
        app.Read("src/App1/App1.csproj").Should().NotContain(tfm);
        app.Read("src/App1.Core/App1.Core.csproj").Should().NotContain(tfm);
        if (workflow is not null)
        {
            app.Exists(workflow).Should().BeFalse();
        }

        foreach (var other in all.Where(p => p != platform))
        {
            app.Read("src/App1/App1.csproj").Should().Contain(other switch
            {
                "android" => "net10.0-android",
                "ios" => "net10.0-ios",
                "windows" => "net10.0-windows",
                "desktop" => "net10.0-desktop",
                _ => "net10.0-browserwasm",
            });
        }
    }

    [TestMethod]
    [DataRow("all", new[] { "android", "ios", "windows", "desktop", "wasm" })]
    [DataRow("nowasm", new[] { "windows", "desktop" })]
    [DataRow("nodesktop", new[] { "wasm", "windows" })]
    [DataRow("wasmdesktop", new[] { "wasm", "desktop" })]
    [DataRow("onlywasm", new[] { "wasm" })]
    [DataRow("onlywin", new[] { "windows" })]
    [DataRow("onlydesktop", new[] { "desktop" })]
    [DataRow("mobile", new[] { "android", "ios" })]
    public void LaunchSettings_StayValidJson(string scenario, string[] platforms)
    {
        var options = platforms.SelectMany(p => new[] { "--platforms", p }).ToArray();

        var app = TemplateFixture.Generate($"launch-{scenario}", "App1", options);

        JsonDocumentOptions strict = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = false };
        var parse = () => JsonDocument.Parse(app.Read("src/App1/Properties/launchSettings.json"), strict).Dispose();
        parse.Should().NotThrow();
    }

    [TestMethod]
    [DataRow("default", new string[0])]
    [DataRow("off", new[] { "--agents", "false", "--spec-kit", "false", "--worktree-identity", "false", "--dev-assets", "false", "--xaml-styler", "false", "--tests", "false", "--ci", "false", "--packaging", "false", "--community", "false" })]
    [DataRow("windows", new[] { "--platforms", "windows" })]
    [DataRow("wasm", new[] { "--platforms", "wasm" })]
    [DataRow("desktop", new[] { "--platforms", "desktop" })]
    [DataRow("android", new[] { "--platforms", "android" })]
    [DataRow("nopackaging", new[] { "--packaging", "false" })]
    [DataRow("noci", new[] { "--ci", "false" })]
    public void MarkdownLinks_ResolveInsideTheApp(string scenario, string[] options)
    {
        var app = TemplateFixture.Generate($"links-{scenario}", "App1", options);
        Regex link = new(@"\]\((?!https?:|#|mailto:)([^)#\s]+)");

        List<string> broken = [];
        foreach (var file in app.AllFiles().Where(f => f.EndsWith(".md", StringComparison.Ordinal) && !IsVendored(f)))
        {
            var directory = Path.GetDirectoryName(app.Path(file))!;
            foreach (Match match in link.Matches(app.Read(file)))
            {
                var target = match.Groups[1].Value;
                if (!File.Exists(Path.Combine(directory, target)) && !Directory.Exists(Path.Combine(directory, target)))
                {
                    broken.Add($"{file} -> {target}");
                }
            }
        }

        broken.Should().BeEmpty();
    }

    private static bool IsVendored(string file) => file.StartsWith(".specify/", StringComparison.Ordinal) || file.Contains("/speckit-", StringComparison.Ordinal);
}
