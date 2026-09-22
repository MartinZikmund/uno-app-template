using AppTemplate.Core.Tests.Fakes;
using AppTemplate.Core.ViewModels;
using FluentAssertions;

namespace AppTemplate.Core.Tests.ViewModels;

[TestClass]
public class WindowShellViewModelTests
{
    [TestMethod]
    public void CanGoBackChanged_RaisedByNavigationService_RefreshesCanGoBackAndNotifies()
    {
        // Regression test: NavigationService.ClearBackStack() (e.g. after onboarding
        // completes) can leave CanGoBack stale because Frame.Navigated fires before
        // the back stack is actually cleared. The service must explicitly re-notify.
        var navigationService = new FakeNavigationService { CanGoBack = true };
        var viewModel = new WindowShellViewModel(navigationService);

        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName);

        navigationService.CanGoBack = false;
        navigationService.RaiseCanGoBackChanged();

        viewModel.CanGoBack.Should().BeFalse();
        raisedProperties.Should().Contain(nameof(WindowShellViewModel.CanGoBack));
    }
}
