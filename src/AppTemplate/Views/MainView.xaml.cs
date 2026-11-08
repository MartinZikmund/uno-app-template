using AppTemplate.Core.Navigation;
using AppTemplate.Core.Services.Tips;
using AppTemplate.Core.ViewModels;
using Microsoft.UI.Dispatching;

namespace AppTemplate.Views;

public partial class MainViewBase : ViewBase<MainViewModel> { }

[NavigationInfo(NavigationSection.Main)]
public sealed partial class MainView : MainViewBase, ITipAnchorHost
{
    private readonly Dictionary<TipId, TeachingTip> _tips;

    public MainView()
    {
        this.InitializeComponent();

        _tips = new()
        {
            [TipId.HomeWelcome] = WelcomeTip,
            [TipId.HomeSampleAction] = SampleActionTip,
            [TipId.HomeCounter] = CounterTip,
        };

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public bool IsAnchorReady(TipId tip) =>
        _tips.TryGetValue(tip, out var teachingTip)
        && teachingTip.Target is { IsLoaded: true, Visibility: Visibility.Visible, ActualWidth: > 0, ActualHeight: > 0 };

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        viewModel.TipAnchors = this;

        // Loaded fires before targets are arranged (ItemsRepeater rows only realize later still),
        // and a tip opened against pre-layout bounds lands in the wrong place. Low priority runs
        // after layout.
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, viewModel.EvaluateTips);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.TipAnchors = null;
        }
    }

    private void Tip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
    {
        // Programmatic closes come from the view model moving on, or from the page unloading;
        // neither means the user dismissed the tip.
        if (args.Reason == TeachingTipCloseReason.Programmatic || ViewModel is null)
        {
            return;
        }

        var tip = _tips.First(pair => pair.Value == sender).Key;
        ViewModel.DismissTipCommand.Execute(tip);
    }
}
