using AppTemplate.Core.Navigation;
using AppTemplate.ViewModels;

namespace AppTemplate.Views;

[NavigationInfo(NavigationSection.Onboarding, NavigationTransition.Suppress)]
public partial class OnboardingViewBase : ViewBase<OnboardingViewModel> { }

public sealed partial class OnboardingView : OnboardingViewBase
{
    public OnboardingView()
    {
        this.InitializeComponent();
    }
}
