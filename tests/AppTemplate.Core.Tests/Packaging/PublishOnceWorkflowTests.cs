using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace AppTemplate.Core.Tests.Packaging;

[TestClass]
public partial class PublishOnceWorkflowTests
{
    // publish-once marks a commit per environment name, so two jobs sharing one would make the second skip as
    // "already published", as the macOS upload would after iOS's if both used "testflight".
    [TestMethod]
    public void ReleaseJobs_EachKeepTheirOwnPublishMarker()
    {
        var yaml = File.ReadAllText(Path.Combine(FindRepoRoot(), ".github", "workflows", "release.yml"));

        var markersByJob = JobHeader().Split(yaml)
            .Skip(1)
            .Chunk(2)
            .Select(job => (Name: job[0], Markers: PublishOnceMarker().Matches(job[1]).Select(m => m.Groups[1].Value).Distinct().ToList()))
            .Where(job => job.Markers.Count > 0)
            .ToList();

        //#if (hasAndroid || hasIos || hasWindows || hasWasm || useMacAppStore)
        markersByJob.Should().NotBeEmpty();
        //#endif
        markersByJob.SelectMany(job => job.Markers).Should().OnlyHaveUniqueItems();
    }

    [GeneratedRegex(@"^  ([a-z0-9-]+):\r?$", RegexOptions.Multiline)]
    private static partial Regex JobHeader();

    [GeneratedRegex(@"uses: \./\.github/actions/publish-once\s+with:\s+(?:mode: record\s+)?environment: (\S+)")]
    private static partial Regex PublishOnceMarker();

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "version.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root (version.json) not found.");
    }
}
