using AppTemplate.Services.Logging;
using FluentAssertions;

namespace AppTemplate.Core.Tests.Services.Logging;

[TestClass]
public class LogPathsTests
{
    [TestMethod]
    public void GetFolder_WithAppDataPath_ReturnsLogsSubfolder()
    {
        var appData = Path.Combine("data", "app");

        var folder = LogPaths.GetFolder(appData);

        folder.Should().Be(Path.Combine(appData, LogPaths.FolderName));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GetFolder_WithoutAppDataPath_Throws(string? appData)
    {
        var act = () => LogPaths.GetFolder(appData!);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void GetFilePath_WithFolder_ReturnsFileInThatFolder()
    {
        var folder = Path.Combine("data", "Logs");

        var file = LogPaths.GetFilePath(folder);

        file.Should().Be(Path.Combine(folder, LogPaths.FileName));
    }

    [TestMethod]
    public void FileSizeLimit_KeepsTotalLogSizeBounded()
    {
        (LogPaths.FileSizeLimitBytes * LogPaths.RetainedFileCountLimit)
            .Should().BeLessThanOrEqualTo(50L * 1024 * 1024);
    }
}
