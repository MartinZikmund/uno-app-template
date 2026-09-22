using AppTemplate.Services.Settings;
using Microsoft.UI.Xaml;

namespace AppTemplate.Core.Tests.Fakes;

internal sealed class FakeAppPreferences : IAppPreferences
{
    public int DataVersion { get; set; }

    public bool FirstStart { get; set; }

    public int LaunchCount { get; set; }

    public bool OfferUserRating { get; set; }

    public ElementTheme Theme { get; set; } = ElementTheme.Default;
}
