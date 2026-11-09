using AwesomeAssertions;
using YamlDotNet.RepresentationModel;

namespace Template.GenerationTests;

/// <summary>
/// nuget.org only trusts a token whose workflow file and environment match its policy, and a mistake here only shows up
/// when someone publishes.
/// </summary>
[TestClass]
public class PublishWorkflowTests
{
    private const string _workflow = ".github/workflows/template-package.yml";

    private static readonly GeneratedApp _repository = new(TemplateFixture.RepoRoot, "AppTemplate");

    [TestMethod]
    public void Workflow_ParsesAndHangsTogether()
    {
        WorkflowTests.CheckWorkflow(_repository, _workflow).Should().BeEmpty();
    }

    [TestMethod]
    public void Workflow_StoresNoApiKey()
    {
        _repository.Read(_workflow).Should().NotContain("secrets.NUGET_API_KEY");
    }

    [TestMethod]
    public void Publish_PushesWithTheKeyFromTheTokenExchange()
    {
        var steps = Steps(Job("publish"));

        var login = steps.Single(step => Scalar(step, "uses")?.StartsWith("NuGet/login@", StringComparison.Ordinal) == true);
        var push = steps.Single(step => Scalar(step, "run")?.Contains("dotnet nuget push", StringComparison.Ordinal) == true);

        var key = ((YamlMappingNode)push["env"])["NUGET_API_KEY"].ToString();
        key.Should().Be($"${{{{ steps.{Scalar(login, "id")}.outputs.NUGET_API_KEY }}}}");
    }

    [TestMethod]
    public void Publish_RunsOnlyWhenAskedTo()
    {
        var job = Job("publish");

        Scalar(job, "if").Should().Contain("workflow_dispatch").And.Contain("inputs.publish");
        Scalar(job, "needs").Should().Be("pack");
    }

    [TestMethod]
    public void Publish_RunsNoRepositoryCode()
    {
        Steps(Job("publish")).Select(step => Scalar(step, "uses")).Should().NotContain(uses => uses != null && uses.StartsWith("actions/checkout@", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OnlyThePublishJob_CanRequestAToken()
    {
        var root = Root();

        Permissions(root).Should().NotContainKey("id-token");
        foreach (var job in ((YamlMappingNode)root["jobs"]).Children)
        {
            var permissions = Permissions((YamlMappingNode)job.Value);
            if (job.Key.ToString() == "publish")
            {
                permissions["id-token"].Should().Be("write");
            }
            else
            {
                permissions.Should().NotContainKey("id-token");
            }
        }
    }

    [TestMethod]
    public void SetupDocs_NameWhatThePolicyHasToMatch()
    {
        var docs = _repository.Read("docs/template-authoring.md");

        docs.Should().Contain($"`{Path.GetFileName(_workflow)}`");
        docs.Should().Contain($"`{Scalar(Job("publish"), "environment")}`");
        docs.Should().NotContain("NUGET_API_KEY");
    }

    private static YamlMappingNode Root()
    {
        YamlStream stream = [];
        stream.Load(new StringReader(_repository.Read(_workflow)));
        return (YamlMappingNode)stream.Documents[0].RootNode;
    }

    private static YamlMappingNode Job(string name) => (YamlMappingNode)((YamlMappingNode)Root()["jobs"])[name];

    private static List<YamlMappingNode> Steps(YamlMappingNode job) => [.. ((YamlSequenceNode)job["steps"]).Cast<YamlMappingNode>()];

    private static string? Scalar(YamlMappingNode node, string key) =>
        node.Children.TryGetValue(key, out var value) ? value.ToString() : null;

    private static Dictionary<string, string> Permissions(YamlMappingNode node) =>
        node.Children.TryGetValue("permissions", out var value)
            ? ((YamlMappingNode)value).Children.ToDictionary(entry => entry.Key.ToString(), entry => entry.Value.ToString())
            : [];
}
