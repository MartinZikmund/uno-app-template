using AppTemplate.Core.Tests.Fakes;
using AppTemplate.Core.ViewModels;
using FluentAssertions;

namespace AppTemplate.Core.Tests.ViewModels;

[TestClass]
public class WindowShellViewModelTests
{
    private static readonly Dictionary<string, string> AppName =
        new() { ["ApplicationName"] = "App Template" };

    [TestMethod]
    public void AppTitle_WhenNotInWorktree_IsTheApplicationName()
    {
        var viewModel = CreateViewModel(worktreeName: null);

        viewModel.AppTitle.Should().Be("App Template");
    }

    [TestMethod]
    public void AppTitle_WhenInWorktree_AppendsTheWorktreeName()
    {
        var viewModel = CreateViewModel(worktreeName: "identity");

        viewModel.AppTitle.Should().Be("App Template (identity)");
    }

    [TestMethod]
    public void AppTitle_WhenWorktreeNameIsEmpty_IsTheApplicationName()
    {
        var viewModel = CreateViewModel(worktreeName: string.Empty);

        viewModel.AppTitle.Should().Be("App Template");
    }

    private static WindowShellViewModel CreateViewModel(string? worktreeName) =>
        new(
            new FakeNavigationService(),
            new FakeStringLocalizer(AppName),
            new FakeApplication { WorktreeName = worktreeName });
}
