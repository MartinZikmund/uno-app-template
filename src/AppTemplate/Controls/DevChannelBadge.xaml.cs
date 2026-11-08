using AppTemplate.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AppTemplate.Controls;

public sealed partial class DevChannelBadge : UserControl
{
    public DevChannelBadge()
    {
        InitializeComponent();

        if (AppEnvironment.IsCiBuild)
        {
            BadgeBorder.Background = (Brush)Resources["CiBadgeBackgroundBrush"];
            BadgeText.Foreground = (Brush)Resources["CiBadgeForegroundBrush"];
        }
    }
}
