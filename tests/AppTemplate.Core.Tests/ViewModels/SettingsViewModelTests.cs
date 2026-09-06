using AppTemplate.Core.Tests.Fakes;
using AppTemplate.Core.ViewModels;
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

    private static SettingsViewModel CreateViewModel(
        string? worktreeName,
        IDictionary<string, string>? strings = null) =>
        new(
            new FakeStringLocalizer(strings),
            new FakeAppPreferences(),
            new FakeThemeManager(),
            new FakePreferences(),
            new FakeApplication { WorktreeName = worktreeName });
}
