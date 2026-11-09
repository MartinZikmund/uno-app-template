using AwesomeAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public class StoreWorkflowTests
{
    // $ErrorActionPreference does not stop on a native non-zero exit, so a failed
    // `msstore submission update` would still be followed by `msstore submission publish`.
    [TestMethod]
    [DataRow("release.yml")]
    [DataRow("store-ops.yml")]
    public void WorkflowScripts_RunningMsstore_StopOnNativeFailure(string workflow)
    {
        var yaml = File.ReadAllText(Path.Combine(FindRepoRoot(), ".github", "workflows", workflow));

        var scripts = yaml.Split("run: |")
            .Skip(1)
            .Where(script => script.Contains("msstore reconfigure"))
            .ToList();

        scripts.Should().NotBeEmpty();
        scripts.Should().AllSatisfy(script => script.Should().Contain("$PSNativeCommandUseErrorActionPreference = $true"));
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root (AGENTS.md) not found.");
    }
}
