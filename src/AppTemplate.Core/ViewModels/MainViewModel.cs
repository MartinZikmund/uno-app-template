using AppTemplate.Core.Services.Tips;

namespace AppTemplate.Core.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    /// <summary>Sample action uses before the contextual tip shows up.</summary>
    public const int CounterTipThreshold = 3;

    private readonly IStringLocalizer _localizer;
    private readonly ITipService _tipService;

    public MainViewModel(IStringLocalizer localizer, ITipService tipService)
    {
        _localizer = localizer;
        _tipService = tipService;
        PageTitle = _localizer["ApplicationName"];
    }

    /// <summary>Set by the view, so tips only open when their target is on screen.</summary>
    public ITipAnchorHost? TipAnchors { get; set; }

    /// <summary>The teaching tip on screen, or null. A single value, so at most one is open.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcomeTipOpen))]
    [NotifyPropertyChangedFor(nameof(IsSampleActionTipOpen))]
    [NotifyPropertyChangedFor(nameof(IsCounterTipOpen))]
    public partial TipId? ActiveTip { get; set; }

    public bool IsWelcomeTipOpen => ActiveTip == TipId.HomeWelcome;

    public bool IsSampleActionTipOpen => ActiveTip == TipId.HomeSampleAction;

    public bool IsCounterTipOpen => ActiveTip == TipId.HomeCounter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SampleActionCountText))]
    public partial int SampleActionCount { get; set; }

    public string SampleActionCountText => _localizer["SampleActionCountFormat", SampleActionCount].Value;

    private bool IsTourPending => TipIdExtensions.TourSteps.Any(_tipService.ShouldShow);

    /// <summary>
    /// Opens the first unseen tour step, if any. A step whose anchor isn't ready sits this
    /// load out without being marked seen.
    /// </summary>
    public void EvaluateTips()
    {
        if (ActiveTip is not null)
        {
            return;
        }

        foreach (var step in TipIdExtensions.TourSteps)
        {
            if (_tipService.ShouldShow(step))
            {
                TryOpen(step);
                return;
            }
        }
    }

    [RelayCommand]
    private void SampleAction()
    {
        SampleActionCount++;

        if (SampleActionCount >= CounterTipThreshold
            && ActiveTip is null
            && !IsTourPending
            && _tipService.ShouldShow(TipId.HomeCounter))
        {
            TryOpen(TipId.HomeCounter);
        }
    }

    [RelayCommand]
    private void TipNext(TipId tip)
    {
        if (ActiveTip != tip)
        {
            return;
        }

        _tipService.MarkSeen(tip);
        ActiveTip = null;

        if (tip.NextTourStep() is { } next)
        {
            TryOpen(next);
        }
    }

    /// <summary>
    /// Closing a tour step ends the tour (Skip); closing any other tip marks just that one.
    /// Ignored unless <paramref name="tip"/> is the active one, so a late Closed event from a
    /// tip the flow already moved past can't end the next step.
    /// </summary>
    [RelayCommand]
    private void DismissTip(TipId tip)
    {
        if (ActiveTip != tip)
        {
            return;
        }

        if (tip.IsTourStep())
        {
            _tipService.MarkSeen([.. TipIdExtensions.TourSteps]);
        }
        else
        {
            _tipService.MarkSeen(tip);
        }

        ActiveTip = null;
    }

    private void TryOpen(TipId tip)
    {
        if (TipAnchors?.IsAnchorReady(tip) == true)
        {
            ActiveTip = tip;
        }
    }
}
