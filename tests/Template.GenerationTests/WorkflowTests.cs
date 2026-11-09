using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using YamlDotNet.RepresentationModel;

namespace Template.GenerationTests;

/// <summary>
/// The release pipeline spans platforms in one file, so removing a head has to leave a workflow that still parses and
/// still hangs together. GitHub only reports a broken one when it runs, and a release is a bad moment to find out.
/// </summary>
[TestClass]
public class WorkflowTests
{
    private static readonly Regex _needsExpression = new(@"needs\.([A-Za-z0-9_-]+)\.");
    private static readonly Regex _script = new(@"python (\.github/scripts/[A-Za-z0-9_.-]+)");

    private static readonly string[] _cloudflareFiles =
    [
        "wrangler.jsonc",
        "docs/deployment.md",
        ".github/actions/prepare-wasm-assets/action.yml",
        ".github/workflows/wasm-pr-preview.yml",
        "src/App1/Platforms/WebAssembly/wwwroot/_headers",
        "tests/App1.Core.Tests/Packaging/CloudflareWorkflowTests.cs",
    ];

    [TestMethod]
    [DataRow("default", new string[0])]
    [DataRow("android", new[] { "--platforms", "android" })]
    [DataRow("ios", new[] { "--platforms", "ios" })]
    [DataRow("windows", new[] { "--platforms", "windows" })]
    [DataRow("desktop", new[] { "--platforms", "desktop" })]
    [DataRow("wasm", new[] { "--platforms", "wasm" })]
    [DataRow("mobile", new[] { "--platforms", "android", "--platforms", "ios" })]
    [DataRow("nopackaging", new[] { "--packaging", "false" })]
    [DataRow("noci", new[] { "--ci", "false" })]
    [DataRow("notests", new[] { "--tests", "false" })]
    [DataRow("nostyler", new[] { "--xaml-styler", "false" })]
    [DataRow("noautomation", new[] { "--ci", "false", "--packaging", "false" })]
    public void Workflows_ParseAndHangTogether(string scenario, string[] options)
    {
        var app = TemplateFixture.Generate($"wf-{scenario}", "App1", options);
        List<string> problems = [];

        var workflows = app.Exists(".github/workflows")
            ? Directory.GetFiles(app.Path(".github/workflows"), "*.yml").Select(path => Path.GetRelativePath(app.Root, path).Replace('\\', '/')).ToList()
            : [];
        foreach (var workflow in workflows)
        {
            problems.AddRange(CheckWorkflow(app, workflow).Select(problem => $"{workflow}: {problem}"));
        }

        problems.AddRange(CheckDependabot(app));

        problems.Should().BeEmpty();
    }

    [TestMethod]
    public void Release_ListsOnlyThePlatformsTheAppHas()
    {
        var app = TemplateFixture.Generate("wf-release-win", "App1", "--platforms", "windows", "--platforms", "desktop");

        var release = app.Read(".github/workflows/release.yml");
        release.Should().Contain("build-windows:").And.Contain("build-desktop:").And.Contain("publish-msstore-draft:").And.Contain("commit-msstore:");
        foreach (var removed in new[] { "build-android", "build-ios", "build-wasm", "publish-play-internal", "promote-play-production", "publish-testflight", "submit-appstore", "publish-web" })
        {
            release.Should().NotContain(removed);
        }

        var main = app.Read(".github/workflows/build-main.yml");
        main.Should().Contain("build-windows:").And.Contain("build-desktop:");
        main.Should().NotContain("build-android").And.NotContain("build-ios").And.NotContain("build-wasm");
    }

    [TestMethod]
    public void StoreOps_OffersOnlyTheStoresTheAppHas()
    {
        var app = TemplateFixture.Generate("wf-ops-android", "App1", "--platforms", "android");

        var ops = app.Read(".github/workflows/store-ops.yml");
        ops.Should().Contain("play-halt").And.Contain("republish");
        ops.Should().NotContain("appstore-").And.NotContain("msstore").And.NotContain("release-windows").And.NotContain("release-wasm");
    }

    [TestMethod]
    public void StoreOps_IsAbsentWithNoStoreToOperate()
    {
        var app = TemplateFixture.Generate("wf-ops-desktop", "App1", "--platforms", "desktop");

        app.Exists(".github/workflows/store-ops.yml").Should().BeFalse();
    }

    [TestMethod]
    public void StoreHealth_ProbesOnlyTheStoresTheAppHas()
    {
        var app = TemplateFixture.Generate("wf-health-win", "App1", "--platforms", "windows");

        var health = app.Read(".github/workflows/store-health.yml");
        health.Should().NotContain("asc_op").And.NotContain("play_track_op");
        health.Should().Contain("Release App token still mints");
    }

    [TestMethod]
    [DataRow("android", ".github/scripts/play_track_op.py")]
    [DataRow("ios", ".github/scripts/asc_op.py")]
    public void StoreScripts_ComeWithTheirPlatformOnly(string platform, string script)
    {
        var withIt = TemplateFixture.Generate($"wf-script-{platform}", "App1", "--platforms", platform);
        var without = TemplateFixture.Generate($"wf-script-no{platform}", "App1", "--platforms", "desktop");

        withIt.Exists(script).Should().BeTrue();
        without.Exists(script).Should().BeFalse();
    }

    [TestMethod]
    public void PackagingOff_RemovesThePipelineAndKeepsWhatCiNeeds()
    {
        var app = TemplateFixture.Generate("wf-off", "App1", "--packaging", "false");

        foreach (var removed in new[]
        {
            ".github/workflows/release.yml", ".github/workflows/build-main.yml", ".github/workflows/prepare-release.yml",
            ".github/workflows/forward-merge.yml", ".github/workflows/store-ops.yml", ".github/workflows/store-health.yml",
            ".github/workflows/wasm-pr-preview.yml", ".github/workflows/_build-android.yml", ".github/scripts",
            ".github/actions/publish-once", "docs/release-pipeline.md", "docs/release-runbook.md",
        })
        {
            app.Exists(removed).Should().BeFalse($"{removed} belongs to the release pipeline");
        }

        app.Exists(".github/workflows/ci.yml").Should().BeTrue();
        app.Exists(".github/actions/setup-dotnet-build/action.yml").Should().BeTrue();
        app.Exists(".github/actions/app-version/action.yml").Should().BeTrue();
    }

    [TestMethod]
    public void CiOff_KeepsWhatThePipelineNeeds()
    {
        var app = TemplateFixture.Generate("wf-ci-off", "App1", "--ci", "false");

        app.Exists(".github/workflows/ci.yml").Should().BeFalse();
        app.Exists(".github/actions/setup-dotnet-build/action.yml").Should().BeTrue();
        app.Exists(".github/workflows/release.yml").Should().BeTrue();
    }

    [TestMethod]
    public void AutomationOff_RemovesTheSharedActions()
    {
        var app = TemplateFixture.Generate("wf-none", "App1", "--ci", "false", "--packaging", "false");

        app.Exists(".github/actions").Should().BeFalse();
    }

    [TestMethod]
    public void WasmOff_RemovesThePreview()
    {
        var app = TemplateFixture.Generate("wf-nowasm", "App1", "--platforms", "windows");

        app.Exists(".github/workflows/wasm-pr-preview.yml").Should().BeFalse();
        app.Exists(".github/workflows/_build-wasm.yml").Should().BeFalse();
    }

    [TestMethod]
    [DataRow("nowasm", new[] { "--platforms", "windows" })]
    [DataRow("nopackaging", new[] { "--packaging", "false" })]
    public void Cloudflare_NothingToDeploy_LeavesNoHostingBehind(string scenario, string[] options)
    {
        var app = TemplateFixture.Generate($"cf-{scenario}", "App1", options);

        foreach (var removed in _cloudflareFiles)
        {
            app.Exists(removed).Should().BeFalse($"{removed} only serves the Cloudflare deployment");
        }

        app.Read("docs/README.md").Should().NotContain("deployment.md");
        foreach (var workflow in app.TextFiles().Where(file => file.StartsWith(".github/", StringComparison.Ordinal)))
        {
            app.Read(workflow).Should().NotContain("wrangler").And.NotContain("CLOUDFLARE_", workflow);
        }
    }

    [TestMethod]
    public void Cloudflare_WasmWithReleaseWorkflows_KeepsHosting()
    {
        var app = TemplateFixture.Generate("cf-wasm", "App1", "--platforms", "wasm");

        foreach (var kept in _cloudflareFiles)
        {
            app.Exists(kept).Should().BeTrue($"{kept} is part of the Cloudflare deployment");
        }

        app.Read("docs/README.md").Should().Contain("deployment.md");
        app.Read(".github/workflows/release.yml").Should().Contain("cloudflare/wrangler-action");
    }

    [TestMethod]
    public void Cloudflare_WasmWithoutReleaseWorkflows_KeepsTheWasmHead()
    {
        var app = TemplateFixture.Generate("cf-wasm-nopack", "App1", "--platforms", "wasm", "--packaging", "false");

        app.Exists("src/App1/Platforms/WebAssembly/Program.cs").Should().BeTrue();
        app.Exists("src/App1/Platforms/WebAssembly/wwwroot/_headers").Should().BeFalse();
    }

    [TestMethod]
    [DataRow("ContosoNotes", "contoso-notes")]
    [DataRow("Contoso.Notes", "contoso-notes")]
    [DataRow("App1", "app1")]
    [DataRow("MY_App", "my-app")]
    public void WorkerName_FollowsTheAppName(string name, string expected)
    {
        var app = TemplateFixture.Generate($"cf-name-{expected}-{name.Length}", name, "--platforms", "wasm");

        WorkerConfig(app).GetProperty("name").GetString().Should().Be(expected);
        app.Read("docs/deployment.md").Should().Contain($"`{expected}`").And.Contain($"https://{expected}.").And.Contain($"-{expected}.");
    }

    [TestMethod]
    public void WorkerName_LongAppName_StaysWithinCloudflaresRules()
    {
        // Cut at 63 characters, right on a separator.
        var app = TemplateFixture.Generate("cf-name-long", new string('A', 62) + "-Notes", "--platforms", "wasm");

        WorkerConfig(app).GetProperty("name").GetString().Should().MatchRegex("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$");
    }

    [TestMethod]
    public void WorkerName_LeavesLinksToTheTemplateAlone()
    {
        var app = TemplateFixture.Generate("cf-name-links", "ContosoNotes");

        app.Read(".template-state.json").Should().Contain("MartinZikmund/uno-app-template");
    }

    [TestMethod]
    public void WorkerConfig_PublishDirectory_IsTheAppsOwn()
    {
        var app = TemplateFixture.Generate("cf-dir", "ContosoNotes", "--platforms", "wasm");

        var directory = WorkerConfig(app).GetProperty("assets").GetProperty("directory").GetString();

        directory.Should().Be("./src/ContosoNotes/bin/Release/net10.0-browserwasm/publish/wwwroot");
    }

    [TestMethod]
    public void DeploymentDoc_NamesNoOtherAccount()
    {
        var app = TemplateFixture.Generate("cf-doc", "ContosoNotes", "--platforms", "wasm");

        app.Read("docs/deployment.md").Should().NotContain("mzikmund");
    }

    [TestMethod]
    [DataRow("nowindows", new[] { "--platforms", "desktop" }, false)]
    [DataRow("nopackaging", new[] { "--platforms", "windows", "--packaging", "false" }, false)]
    [DataRow("windows", new[] { "--platforms", "windows" }, true)]
    public void WindowsSigningTest_ComesWithItsWorkflow(string scenario, string[] options, bool expected)
    {
        var app = TemplateFixture.Generate($"sign-{scenario}", "App1", options);

        app.Exists("tests/App1.Core.Tests/Packaging/WindowsSigningWorkflowTests.cs").Should().Be(expected);
    }

    [TestMethod]
    public void WindowsOff_RemovesItsPackagingDoc()
    {
        var app = TemplateFixture.Generate("wf-nowindows", "App1", "--platforms", "desktop");

        app.Exists("docs/windows-packaging.md").Should().BeFalse();
    }

    [TestMethod]
    public void ToolManifest_Default_HasBothTools()
    {
        var app = TemplateFixture.Generate("tools-default", "App1");

        var tools = ToolsIn(app);
        tools.Should().Contain("nbgv").And.Contain("xamlstyler.console");
    }

    [TestMethod]
    public void ToolManifest_StylerOff_KeepsNbgvBecauseTheWorkflowsCallIt()
    {
        var app = TemplateFixture.Generate("tools-nostyler", "App1", "--xaml-styler", "false");

        var tools = ToolsIn(app);
        tools.Should().Contain("nbgv").And.NotContain("xamlstyler.console");
    }

    [TestMethod]
    public void ToolManifest_NoStylerVariant_MatchesTheRepositorysNbgvVersion()
    {
        var app = TemplateFixture.Generate("tools-version", "App1", "--xaml-styler", "false");
        using var repository = JsonDocument.Parse(File.ReadAllText(Path.Combine(TemplateFixture.RepoRoot, ".config", "dotnet-tools.json")));
        using var generated = JsonDocument.Parse(app.Read(".config/dotnet-tools.json"));

        var expected = repository.RootElement.GetProperty("tools").GetProperty("nbgv").GetProperty("version").GetString();

        generated.RootElement.GetProperty("tools").GetProperty("nbgv").GetProperty("version").GetString().Should().Be(expected);
    }

    [TestMethod]
    public void ToolManifest_NothingUsesIt_IsAbsent()
    {
        var app = TemplateFixture.Generate("tools-none", "App1", "--xaml-styler", "false", "--ci", "false", "--packaging", "false");

        app.Exists(".config/dotnet-tools.json").Should().BeFalse();
    }

    private static JsonElement WorkerConfig(GeneratedApp app)
    {
        using var config = JsonDocument.Parse(app.Read("wrangler.jsonc"), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        return config.RootElement.Clone();
    }

    private static IReadOnlyList<string> ToolsIn(GeneratedApp app)
    {
        using var manifest = JsonDocument.Parse(app.Read(".config/dotnet-tools.json"));
        return [.. manifest.RootElement.GetProperty("tools").EnumerateObject().Select(tool => tool.Name)];
    }

    internal static IEnumerable<string> CheckWorkflow(GeneratedApp app, string workflow)
    {
        var text = app.Read(workflow);
        if (Regex.IsMatch(text, @"^\s*#(if|else|elseif|endif)\b", RegexOptions.Multiline))
        {
            // A stray marker is a valid YAML comment, so nothing else would notice.
            return ["still has a conditional marker"];
        }

        YamlMappingNode root;
        try
        {
            YamlStream stream = [];
            stream.Load(new StringReader(text));
            root = (YamlMappingNode)stream.Documents[0].RootNode;
        }
        catch (Exception exception) when (exception is YamlDotNet.Core.YamlException or InvalidCastException)
        {
            return [$"doesn't parse: {exception.Message}"];
        }

        List<string> problems = [];

        foreach (Match match in _script.Matches(text))
        {
            if (!app.Exists(match.Groups[1].Value))
            {
                problems.Add($"runs {match.Groups[1].Value}, which isn't here");
            }
        }

        foreach (var local in Descendants(root).OfType<YamlMappingNode>().SelectMany(node => node.Children)
                     .Where(entry => entry.Key.ToString() == "uses" && entry.Value.ToString().StartsWith("./", StringComparison.Ordinal))
                     .Select(entry => entry.Value.ToString()[2..]))
        {
            if (!app.Exists(local))
            {
                problems.Add($"uses {local}, which isn't here");
            }
        }

        foreach (var options in Descendants(root).OfType<YamlMappingNode>().SelectMany(node => node.Children)
                     .Where(entry => entry.Key.ToString() == "options"))
        {
            if (options.Value is not YamlSequenceNode { Children.Count: > 0 })
            {
                problems.Add("has an empty choice list");
            }
        }

        if (!root.Children.TryGetValue("jobs", out var jobsNode) || jobsNode is not YamlMappingNode { Children.Count: > 0 } jobs)
        {
            problems.Add("has no jobs");
            return problems;
        }

        var jobNames = jobs.Children.Keys.Select(key => key.ToString()).ToHashSet();
        foreach (var (name, jobNode) in jobs.Children.Select(entry => (entry.Key.ToString(), entry.Value)))
        {
            HashSet<string> needs = [];
            if (jobNode is YamlMappingNode job && job.Children.TryGetValue("needs", out var needsNode))
            {
                needs = needsNode is YamlSequenceNode list ? [.. list.Children.Select(item => item.ToString())] : [needsNode.ToString()];
            }

            problems.AddRange(needs.Where(need => !jobNames.Contains(need)).Select(need => $"job {name} needs {need}, which doesn't exist"));

            // GitHub rejects a whole workflow when an expression names a job outside `needs`.
            var body = Scalars(jobNode);
            foreach (var referenced in body.SelectMany(scalar => _needsExpression.Matches(scalar)).Select(match => match.Groups[1].Value).Distinct())
            {
                if (!needs.Contains(referenced))
                {
                    problems.Add($"job {name} reads needs.{referenced} without needing it");
                }
            }
        }

        return problems;
    }

    private static IEnumerable<string> CheckDependabot(GeneratedApp app)
    {
        if (!app.Exists(".github/dependabot.yml"))
        {
            yield break;
        }

        foreach (Match match in Regex.Matches(app.Read(".github/dependabot.yml"), @"- ""(/[^""]*)"""))
        {
            var directory = match.Groups[1].Value.TrimStart('/');
            if (directory.Length > 0 && !app.Exists(directory))
            {
                yield return $"dependabot.yml watches {directory}, which isn't here";
            }
        }
    }

    private static IEnumerable<YamlNode> Descendants(YamlNode node)
    {
        yield return node;
        IEnumerable<YamlNode> children = node switch
        {
            YamlMappingNode mapping => mapping.Children.Values,
            YamlSequenceNode sequence => sequence.Children,
            _ => [],
        };
        foreach (var descendant in children.SelectMany(Descendants))
        {
            yield return descendant;
        }
    }

    private static IEnumerable<string> Scalars(YamlNode node) =>
        Descendants(node).OfType<YamlScalarNode>().Select(scalar => scalar.Value ?? "");
}
