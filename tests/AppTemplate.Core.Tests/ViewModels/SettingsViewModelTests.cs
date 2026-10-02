using AppTemplate.Core.Tests.Fakes;
using AppTemplate.Core.ViewModels;
using AppTemplate.Services.Logging;
using FluentAssertions;

namespace AppTemplate.Core.Tests.ViewModels;

[TestClass]
public class SettingsViewModelTests
{
    private const string WorktreeFormatKey = "WorktreeFormat";

    [TestMethod]
    public void WorktreeLabel_WhenNotInWorktree_ReturnsNull()
    {
        var viewModel = CreateViewModel(worktreeName: null);

        viewModel.WorktreeLabel.Should().BeNull();
    }

    [TestMethod]
    public void WorktreeLabel_WhenWorktreeNameIsEmpty_ReturnsNull()
    {
        var viewModel = CreateViewModel(worktreeName: string.Empty);

        viewModel.WorktreeLabel.Should().BeNull();
    }

    [TestMethod]
    public void WorktreeLabel_WhenInWorktree_IncludesTheWorktreeName()
    {
        var viewModel = CreateViewModel(
            worktreeName: "identity",
            strings: new Dictionary<string, string> { [WorktreeFormatKey] = "Worktree: {0}" });

        viewModel.WorktreeLabel.Should().Be("Worktree: identity");
    }

    [TestMethod]
    public void WorktreeLabel_WhenInWorktree_UsesTheLocalizedFormatString()
    {
        var viewModel = CreateViewModel(
            worktreeName: "identity",
            strings: new Dictionary<string, string> { [WorktreeFormatKey] = "Pracovni strom: {0}" });

        viewModel.WorktreeLabel.Should().Be("Pracovni strom: identity");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CanOpenLogsFolder_FollowsLauncherSupport(bool isSupported)
    {
        var viewModel = CreateViewModel(folderLauncher: new FakeFolderLauncher { IsSupported = isSupported });

        viewModel.CanOpenLogsFolder.Should().Be(isSupported);
    }

    [TestMethod]
    public async Task OpenLogsFolderCommand_OpensTheLogFolder()
    {
        FakeFolderLauncher launcher = new();
        var viewModel = CreateViewModel(folderLauncher: launcher);

        await viewModel.OpenLogsFolderCommand.ExecuteAsync(null);

        launcher.OpenedPaths.Should().Equal(LogFolder);
    }

    [TestMethod]
    public async Task OpenLogsFolderCommand_WhenLaunchSucceeds_ShowsNoError()
    {
        FakeErrorDialogService errorDialog = new();
        var viewModel = CreateViewModel(errorDialog: errorDialog);

        await viewModel.OpenLogsFolderCommand.ExecuteAsync(null);

        errorDialog.Shown.Should().BeEmpty();
    }

    [TestMethod]
    public async Task OpenLogsFolderCommand_WhenLaunchFails_ShowsErrorNamingTheFolder()
    {
        FakeErrorDialogService errorDialog = new();
        var viewModel = CreateViewModel(
            folderLauncher: new FakeFolderLauncher { Succeeds = false },
            errorDialog: errorDialog,
            strings: new Dictionary<string, string> { ["OpenLogsFolderFailedFormat"] = "Open it yourself: {0}" });

        await viewModel.OpenLogsFolderCommand.ExecuteAsync(null);

        errorDialog.Shown.Should().ContainSingle()
            .Which.Message.Should().Be($"Open it yourself: {LogFolder}");
    }

    private const string LogFolder = "/data/Logs";

    private static SettingsViewModel CreateViewModel(
        string? worktreeName = null,
        IDictionary<string, string>? strings = null,
        FakeFolderLauncher? folderLauncher = null,
        FakeErrorDialogService? errorDialog = null) =>
        new(
            new FakeStringLocalizer(strings),
            new FakeAppPreferences(),
            new FakeThemeManager(),
            new FakePreferences(),
            new FakeApplication { WorktreeName = worktreeName },
            folderLauncher ?? new FakeFolderLauncher(),
            new LogFolderProvider(LogFolder),
            errorDialog ?? new FakeErrorDialogService());
}
