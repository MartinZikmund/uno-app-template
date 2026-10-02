using AppTemplate.Core.Services.Tips;
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

    [TestMethod]
    public void ShowTipsAgain_AfterTipsWereSeen_MakesEveryTipEligibleAgain()
    {
        FakePreferences preferences = new();
        TipService tips = new(preferences);
        tips.MarkSeen([.. Enum.GetValues<TipId>()]);
        var viewModel = CreateViewModel(worktreeName: null, preferences: preferences, tips: tips);

        viewModel.ShowTipsAgainCommand.Execute(null);

        Enum.GetValues<TipId>().Should().OnlyContain(tip => tips.ShouldShow(tip));
    }

    [TestMethod]
    public void ShowTipsAgain_Always_KeepsOtherPreferences()
    {
        FakePreferences preferences = new();
        preferences.Set("SomethingElse", 42);
        TipService tips = new(preferences);
        tips.MarkSeen(TipId.HomeWelcome);
        var viewModel = CreateViewModel(worktreeName: null, preferences: preferences, tips: tips);

        viewModel.ShowTipsAgainCommand.Execute(null);

        preferences.Get("SomethingElse", 0).Should().Be(42, "replaying tips is not a settings reset");
        preferences.ClearCalled.Should().BeFalse();
    }

    private static SettingsViewModel CreateViewModel(
        string? worktreeName,
        IDictionary<string, string>? strings = null,
        FakePreferences? preferences = null,
        ITipService? tips = null)
    {
        preferences ??= new();
        return new(
            new FakeStringLocalizer(strings),
            new FakeAppPreferences(),
            new FakeThemeManager(),
            preferences,
            new FakeApplication { WorktreeName = worktreeName },
            tips ?? new TipService(preferences));
    }
}
