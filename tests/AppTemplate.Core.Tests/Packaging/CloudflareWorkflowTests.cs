using FluentAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public class CloudflareWorkflowTests
{
    [TestMethod]
    [DataRow("release.yml")]
    [DataRow("wasm-pr-preview.yml")]
    public void HasCloudflare_RequiresTheWholeCredentialSet(string workflow)
    {
        var yaml = File.ReadAllText(Path.Combine(FindRepoRoot(), ".github", "workflows", workflow));

        var line = yaml.Split('\n').Single(l => l.TrimStart().StartsWith("HAS_CLOUDFLARE:", StringComparison.Ordinal));

        line.Should().Contain("secrets.CLOUDFLARE_API_TOKEN != ''").And.Contain("secrets.CLOUDFLARE_ACCOUNT_ID != ''");
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
