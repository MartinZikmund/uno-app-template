namespace AppTemplate.Services.Settings;

public interface IAppPreferences
{
    int DataVersion { get; set; }

    bool FirstStart { get; set; }

    bool HasSeenOnboarding { get; set; }

    int LaunchCount { get; set; }

    bool OfferUserRating { get; set; }

    ElementTheme Theme { get; set; }
}
