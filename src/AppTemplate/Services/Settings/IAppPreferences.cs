using Microsoft.UI.Xaml;

namespace AppTemplate.Services.Settings;

public interface IAppPreferences
{
    ElementTheme Theme { get; set; }

    bool FirstStart { get; set; }

    int LaunchCount { get; set; }
}
