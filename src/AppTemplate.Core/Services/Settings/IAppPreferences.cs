namespace AppTemplate.Services.Settings;

public interface IAppPreferences
{
    int DataVersion { get; set; }

    bool FirstStart { get; set; }

    int LaunchCount { get; set; }

    //#if (rating)
    bool OfferUserRating { get; set; }
    //#endif

    ElementTheme Theme { get; set; }
}
