namespace AppTemplate.Core.Services.Tips;

/// <summary>The teaching tips the app can show.</summary>
public enum TipId
{
    /// <summary>Tour step 1: the home page hero.</summary>
    HomeWelcome,

    /// <summary>Tour step 2: the sample action button.</summary>
    HomeSampleAction,

    /// <summary>Contextual: shown once the sample action has been used a few times.</summary>
    HomeCounter,
}

public static class TipIdExtensions
{
    /// <summary>The tour steps, in the order they are shown.</summary>
    public static IReadOnlyList<TipId> TourSteps => _tourSteps;

    private static readonly TipId[] _tourSteps = [TipId.HomeWelcome, TipId.HomeSampleAction];

    /// <summary>
    /// The id written to storage. Ordinals never are, so reordering or inserting a member
    /// cannot change which tips a user has already seen.
    /// </summary>
    public static string ToStorageId(this TipId tip) => tip switch
    {
        TipId.HomeWelcome => "home-welcome",
        TipId.HomeSampleAction => "home-sample-action",
        TipId.HomeCounter => "home-counter",
        _ => throw new ArgumentOutOfRangeException(nameof(tip), tip, "Unknown tip."),
    };

    public static bool IsTourStep(this TipId tip) => TourSteps.Contains(tip);

    /// <summary>The tour step after <paramref name="tip"/>, or null at the end of the tour.</summary>
    public static TipId? NextTourStep(this TipId tip)
    {
        var index = Array.IndexOf(_tourSteps, tip);
        return index >= 0 && index < _tourSteps.Length - 1 ? _tourSteps[index + 1] : null;
    }
}
