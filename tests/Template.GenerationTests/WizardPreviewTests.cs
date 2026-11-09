using System.Text.Json;
using AwesomeAssertions;
using YamlDotNet.RepresentationModel;

namespace Template.GenerationTests;

[TestClass]
public class WizardPreviewTests
{
    private const string _workflow = ".github/workflows/template-wizard-preview.yml";
    private const string _workerConfig = "tools/Wizard.Ui/wrangler.jsonc";

    private static readonly GeneratedApp _repository = new(TemplateFixture.RepoRoot, "AppTemplate");

    [TestMethod]
    public void Workflow_ParsesAndHangsTogether()
    {
        WorkflowTests.CheckWorkflow(_repository, _workflow).Should().BeEmpty();
    }

    [TestMethod]
    public void Workflow_UsesTheWorkerItsConfigNames()
    {
        var env = JobEnvironment();

        env["WORKER"].Should().Be(Config().GetProperty("name").GetString());
    }

    [TestMethod]
    public void Workflow_PublishesWhereTheConfigUploadsFrom()
    {
        var uploaded = Path.GetFullPath(Path.Combine(TemplateFixture.RepoRoot, "tools", "Wizard.Ui", Config().GetProperty("assets").GetProperty("directory").GetString()!));

        var published = Path.GetFullPath(Path.Combine(TemplateFixture.RepoRoot, JobEnvironment()["PUBLISH_DIR"]));

        published.Should().Be(uploaded);
    }

    [TestMethod]
    public void WorkerConfig_IsSeparateFromTheApps()
    {
        using var app = JsonDocument.Parse(_repository.Read("wrangler.jsonc"), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

        Config().GetProperty("name").GetString().Should().NotBe(app.RootElement.GetProperty("name").GetString());
    }

    [TestMethod]
    public void WorkerConfig_ServesPreviewsOnly()
    {
        var config = Config();

        config.GetProperty("workers_dev").GetBoolean().Should().BeFalse("the first upload shouldn't leave one PR's wizard live for good");
        config.GetProperty("preview_urls").GetBoolean().Should().BeTrue();
    }

    [TestMethod]
    public void GeneratedApp_GetsNoneOfIt()
    {
        var app = TemplateFixture.Generate("wizard-preview", "ContosoNotes");

        app.Exists(_workflow).Should().BeFalse();
        app.Exists(_workerConfig).Should().BeFalse();
        app.TextFiles().Where(file => app.Read(file).Contains("uno-app-wizard", StringComparison.Ordinal)).Should().BeEmpty();
    }

    private static JsonElement Config()
    {
        using var config = JsonDocument.Parse(_repository.Read(_workerConfig), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        return config.RootElement.Clone();
    }

    private static Dictionary<string, string> JobEnvironment()
    {
        YamlStream stream = [];
        stream.Load(new StringReader(_repository.Read(_workflow)));
        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        var job = (YamlMappingNode)((YamlMappingNode)root["jobs"]).Children.Single().Value;
        return ((YamlMappingNode)job["env"]).Children.ToDictionary(entry => entry.Key.ToString(), entry => entry.Value.ToString());
    }
}
