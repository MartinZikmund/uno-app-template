using AppTemplate.Core.Services.Tips;
using AppTemplate.Core.Tests.Fakes;
using AppTemplate.Core.ViewModels;
using FluentAssertions;

namespace AppTemplate.Core.Tests.ViewModels;

[TestClass]
public class MainViewModelTipTests
{
    private TipService _tips = null!;
    private FakeTipAnchorHost _anchors = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tips = new(new FakePreferences());
        _anchors = new();
    }

    private MainViewModel CreateViewModel() => new(new FakeStringLocalizer(), _tips) { TipAnchors = _anchors };

    private void CompleteTheTour() => _tips.MarkSeen([.. TipIdExtensions.TourSteps]);

    private static void ClickSampleAction(MainViewModel viewModel, int times)
    {
        for (var i = 0; i < times; i++)
        {
            viewModel.SampleActionCommand.Execute(null);
        }
    }

    [TestMethod]
    public void EvaluateTips_NothingSeen_OpensTheFirstTourStep()
    {
        var viewModel = CreateViewModel();

        viewModel.EvaluateTips();

        viewModel.ActiveTip.Should().Be(TipId.HomeWelcome);
        viewModel.IsWelcomeTipOpen.Should().BeTrue();
        viewModel.IsSampleActionTipOpen.Should().BeFalse();
        viewModel.IsCounterTipOpen.Should().BeFalse();
    }

    [TestMethod]
    public void EvaluateTips_FirstStepSeen_OpensTheSecondStep()
    {
        _tips.MarkSeen(TipId.HomeWelcome);
        var viewModel = CreateViewModel();

        viewModel.EvaluateTips();

        viewModel.ActiveTip.Should().Be(TipId.HomeSampleAction);
    }

    [TestMethod]
    public void EvaluateTips_TourDone_OpensNothing()
    {
        CompleteTheTour();
        var viewModel = CreateViewModel();

        viewModel.EvaluateTips();

        viewModel.ActiveTip.Should().BeNull("the contextual tip waits for its moment");
    }

    [TestMethod]
    public void EvaluateTips_AnchorMissing_OpensNothingAndKeepsTheTipUnseen()
    {
        _anchors.MissingAnchors.Add(TipId.HomeWelcome);
        var viewModel = CreateViewModel();

        viewModel.EvaluateTips();

        viewModel.ActiveTip.Should().BeNull();
        _tips.ShouldShow(TipId.HomeWelcome).Should().BeTrue("a tip nobody saw must not be burned");
    }

    [TestMethod]
    public void EvaluateTips_NoAnchorHost_OpensNothing()
    {
        var viewModel = new MainViewModel(new FakeStringLocalizer(), _tips);

        viewModel.EvaluateTips();

        viewModel.ActiveTip.Should().BeNull();
    }

    [TestMethod]
    public void EvaluateTips_TipAlreadyOpen_LeavesItAlone()
    {
        var viewModel = CreateViewModel();
        viewModel.EvaluateTips();
        viewModel.TipNextCommand.Execute(null);

        viewModel.EvaluateTips();

        viewModel.ActiveTip.Should().Be(TipId.HomeSampleAction);
    }

    [TestMethod]
    public void TipNext_OnTheFirstStep_MarksItSeenAndOpensTheSecond()
    {
        var viewModel = CreateViewModel();
        viewModel.EvaluateTips();

        viewModel.TipNextCommand.Execute(null);

        viewModel.ActiveTip.Should().Be(TipId.HomeSampleAction);
        _tips.ShouldShow(TipId.HomeWelcome).Should().BeFalse();
        _tips.ShouldShow(TipId.HomeSampleAction).Should().BeTrue();
    }

    [TestMethod]
    public void TipNext_NextAnchorMissing_ClosesAndKeepsTheNextStepUnseen()
    {
        var viewModel = CreateViewModel();
        viewModel.EvaluateTips();
        _anchors.MissingAnchors.Add(TipId.HomeSampleAction);

        viewModel.TipNextCommand.Execute(null);

        viewModel.ActiveTip.Should().BeNull();
        _tips.ShouldShow(TipId.HomeWelcome).Should().BeFalse();
        _tips.ShouldShow(TipId.HomeSampleAction).Should().BeTrue();
    }

    [TestMethod]
    public void DismissTip_OnTheFirstStep_SkipsTheWholeTour()
    {
        var viewModel = CreateViewModel();
        viewModel.EvaluateTips();

        viewModel.DismissTipCommand.Execute(TipId.HomeWelcome);

        viewModel.ActiveTip.Should().BeNull();
        TipIdExtensions.TourSteps.Should().OnlyContain(step => !_tips.ShouldShow(step));
    }

    [TestMethod]
    public void DismissTip_SkippingTheTour_LeavesTheContextualTipEligible()
    {
        var viewModel = CreateViewModel();
        viewModel.EvaluateTips();

        viewModel.DismissTipCommand.Execute(TipId.HomeWelcome);

        _tips.ShouldShow(TipId.HomeCounter).Should().BeTrue("Skip ends the tour, not every tip");
    }

    [TestMethod]
    public void DismissTip_OnTheLastStep_EndsTheTour()
    {
        var viewModel = CreateViewModel();
        viewModel.EvaluateTips();
        viewModel.TipNextCommand.Execute(null);

        viewModel.DismissTipCommand.Execute(TipId.HomeSampleAction);

        viewModel.ActiveTip.Should().BeNull();
        _tips.ShouldShow(TipId.HomeSampleAction).Should().BeFalse();
    }

    [TestMethod]
    public void DismissTip_ForATipThatIsNotActive_IsIgnored()
    {
        var viewModel = CreateViewModel();
        viewModel.EvaluateTips();
        viewModel.TipNextCommand.Execute(null);

        // The first tip's Closed event arrives after the VM has already moved on.
        viewModel.DismissTipCommand.Execute(TipId.HomeWelcome);

        viewModel.ActiveTip.Should().Be(TipId.HomeSampleAction);
        _tips.ShouldShow(TipId.HomeSampleAction).Should().BeTrue();
    }

    [TestMethod]
    public void SampleAction_ReachingTheThresholdWithTheTourDone_OpensTheContextualTip()
    {
        CompleteTheTour();
        var viewModel = CreateViewModel();

        ClickSampleAction(viewModel, MainViewModel.CounterTipThreshold);

        viewModel.ActiveTip.Should().Be(TipId.HomeCounter);
        viewModel.IsCounterTipOpen.Should().BeTrue();
    }

    [TestMethod]
    public void SampleAction_BelowTheThreshold_OpensNothing()
    {
        CompleteTheTour();
        var viewModel = CreateViewModel();

        ClickSampleAction(viewModel, MainViewModel.CounterTipThreshold - 1);

        viewModel.ActiveTip.Should().BeNull();
    }

    [TestMethod]
    public void SampleAction_WhileTheTourIsPending_DoesNotOpenTheContextualTip()
    {
        var viewModel = CreateViewModel();

        ClickSampleAction(viewModel, MainViewModel.CounterTipThreshold);

        viewModel.ActiveTip.Should().BeNull("the tour drains before any contextual tip fires");
    }

    [TestMethod]
    public void SampleAction_ContextualTipAlreadySeen_OpensNothing()
    {
        CompleteTheTour();
        _tips.MarkSeen(TipId.HomeCounter);
        var viewModel = CreateViewModel();

        ClickSampleAction(viewModel, MainViewModel.CounterTipThreshold);

        viewModel.ActiveTip.Should().BeNull();
    }

    [TestMethod]
    public void SampleAction_ContextualAnchorMissing_OpensNothingAndKeepsTheTipUnseen()
    {
        CompleteTheTour();
        _anchors.MissingAnchors.Add(TipId.HomeCounter);
        var viewModel = CreateViewModel();

        ClickSampleAction(viewModel, MainViewModel.CounterTipThreshold);

        viewModel.ActiveTip.Should().BeNull();
        _tips.ShouldShow(TipId.HomeCounter).Should().BeTrue();
    }

    [TestMethod]
    public void DismissTip_OnTheContextualTip_MarksOnlyItSeen()
    {
        CompleteTheTour();
        var viewModel = CreateViewModel();
        ClickSampleAction(viewModel, MainViewModel.CounterTipThreshold);

        viewModel.DismissTipCommand.Execute(TipId.HomeCounter);

        viewModel.ActiveTip.Should().BeNull();
        _tips.ShouldShow(TipId.HomeCounter).Should().BeFalse();
    }

    [TestMethod]
    public void SampleAction_Always_UpdatesTheCountText()
    {
        var viewModel = new MainViewModel(
            new FakeStringLocalizer(new Dictionary<string, string> { ["SampleActionCountFormat"] = "Clicks: {0}" }),
            _tips);

        ClickSampleAction(viewModel, 2);

        viewModel.SampleActionCountText.Should().Be("Clicks: 2");
    }
}
