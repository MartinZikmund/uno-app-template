using AppTemplate.Core.Models;
using AwesomeAssertions;

namespace AppTemplate.Core.Tests.Models;

[TestClass]
public class RepositoryLinksTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("not a url")]
    [DataRow("ftp://github.com/owner/repo")]
    public void IsAvailable_WhenUrlIsMissingOrInvalid_IsFalse(string? url)
    {
        RepositoryLinks links = new(url);

        links.IsAvailable.Should().BeFalse();
        links.RepositoryUri.Should().BeNull();
        links.ReportProblemUri.Should().BeNull();
    }

    [TestMethod]
    [DataRow("https://github.com/owner/repo")]
    [DataRow("https://github.com/owner/repo/")]
    [DataRow("  https://github.com/owner/repo  ")]
    public void Links_WhenUrlIsValid_PointToRepositoryAndNewIssue(string url)
    {
        RepositoryLinks links = new(url);

        links.IsAvailable.Should().BeTrue();
        links.RepositoryUri.Should().Be(new Uri("https://github.com/owner/repo"));
        links.ReportProblemUri.Should().Be(new Uri("https://github.com/owner/repo/issues/new"));
    }
}
