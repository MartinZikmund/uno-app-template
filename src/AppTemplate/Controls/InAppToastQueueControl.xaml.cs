using System.Globalization;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace AppTemplate.Controls;

/// <summary>
/// A generic in-app toast/notification control that shows queued messages one at a time
/// with slide-in/out animations, an auto-hide timer and hover-to-pause behavior.
/// </summary>
/// <remarks>
/// Place a single instance in a high z-order layer of your page (e.g. as the last child of the
/// root <c>Grid</c> so it overlays the page content) and call <see cref="Enqueue(string, string, string?)"/>
/// whenever you want to surface feedback such as "achievement unlocked" or "sync completed".
/// <example>
/// XAML:
/// <code>
/// &lt;Grid&gt;
///     &lt;!-- page content --&gt;
///     &lt;controls:InAppToastQueueControl x:Name="Toasts" /&gt;
/// &lt;/Grid&gt;
/// </code>
/// Code-behind:
/// <code>
/// Toasts.Enqueue("Achievement unlocked", "You watered your plant 7 days in a row!", "#2E7D32");
/// Toasts.Enqueue("Sync completed", "All changes saved to the cloud.");
/// </code>
/// </example>
/// </remarks>
public sealed partial class InAppToastQueueControl : UserControl
{
    private static readonly TimeSpan AutoHideDelay = TimeSpan.FromSeconds(4);
    private static readonly Color DefaultBadgeColor = Color.FromArgb(255, 0x88, 0x88, 0x88);

    private readonly Queue<ToastInfo> _pending = new();
    private DispatcherTimer? _autoHideTimer;
    private bool _isShowing;
    private bool _isPaused;

    public InAppToastQueueControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Enqueues a toast to be shown. Toasts are displayed one at a time in FIFO order; if one is
    /// already visible the new toast is shown once the current one auto-hides or is dismissed.
    /// </summary>
    /// <param name="title">The bold title line.</param>
    /// <param name="message">The secondary message line.</param>
    /// <param name="badgeColorHex">Optional badge background color as a hex string (e.g. <c>#RRGGBB</c> or <c>#AARRGGBB</c>). Falls back to the accent badge when null/invalid.</param>
    public void Enqueue(string title, string message, string? badgeColorHex = null)
        => Enqueue(new ToastInfo(title, message, badgeColorHex));

    /// <summary>
    /// Enqueues a toast to be shown.
    /// </summary>
    public void Enqueue(ToastInfo toast)
    {
        ArgumentNullException.ThrowIfNull(toast);

        _pending.Enqueue(toast);

        if (!_isShowing)
        {
            ShowNext();
        }
    }

    private void ShowNext()
    {
        if (_pending.Count == 0)
        {
            _isShowing = false;
            return;
        }

        _isShowing = true;
        var toast = _pending.Dequeue();

        TitleText.Text = toast.Title;
        MessageText.Text = toast.Message;
        MessageText.Visibility = string.IsNullOrEmpty(toast.Message) ? Visibility.Collapsed : Visibility.Visible;

        BadgeBorder.Background = new SolidColorBrush(
            TryParseHexColor(toast.BadgeColorHex, out var color) ? color : DefaultBadgeColor);

        RootGrid.Visibility = Visibility.Visible;
        AnimateIn();
        RestartAutoHideTimer();
    }

    private void RestartAutoHideTimer()
    {
        _autoHideTimer?.Stop();
        _autoHideTimer = new DispatcherTimer { Interval = AutoHideDelay };
        _autoHideTimer.Tick += AutoHideTimer_Tick;
        _isPaused = false;
        _autoHideTimer.Start();
    }

    private void AutoHideTimer_Tick(object? sender, object e)
    {
        _autoHideTimer?.Stop();
        Hide();
    }

    private void Hide()
    {
        AnimateOut(() =>
        {
            if (_pending.Count > 0)
            {
                ShowNext();
            }
            else
            {
                _isShowing = false;
            }
        });
    }

    private void AnimateIn()
    {
        var storyboard = new Storyboard();

        var slide = new DoubleAnimation
        {
            From = -100,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(slide, SlideTransform);
        Storyboard.SetTargetProperty(slide, "Y");
        storyboard.Children.Add(slide);

        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
        };
        Storyboard.SetTarget(fade, RootGrid);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        storyboard.Begin();
    }

    private void AnimateOut(Action onCompleted)
    {
        var storyboard = new Storyboard();

        var slide = new DoubleAnimation
        {
            From = 0,
            To = -100,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        };
        Storyboard.SetTarget(slide, SlideTransform);
        Storyboard.SetTargetProperty(slide, "Y");
        storyboard.Children.Add(slide);

        var fade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
        };
        Storyboard.SetTarget(fade, RootGrid);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        storyboard.Completed += (_, _) =>
        {
            RootGrid.Visibility = Visibility.Collapsed;
            onCompleted();
        };

        storyboard.Begin();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _autoHideTimer?.Stop();
        Hide();
    }

    // Hover-to-pause: keep the toast visible while the pointer is over it.
    private void Toast_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (_isShowing && !_isPaused)
        {
            _isPaused = true;
            _autoHideTimer?.Stop();
        }
    }

    private void Toast_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (_isShowing && _isPaused)
        {
            _isPaused = false;
            RestartAutoHideTimer();
        }
    }

    /// <summary>
    /// Parses a hex color string in the form <c>#RGB</c>, <c>#RRGGBB</c> or <c>#AARRGGBB</c>
    /// (the leading <c>#</c> is optional).
    /// </summary>
    private static bool TryParseHexColor(string? hex, out Color color)
    {
        color = DefaultBadgeColor;

        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        var value = hex.TrimStart('#');

        // Expand shorthand #RGB to #RRGGBB.
        if (value.Length == 3)
        {
            value = string.Concat(value[0], value[0], value[1], value[1], value[2], value[2]);
        }

        if (value.Length is not (6 or 8))
        {
            return false;
        }

        byte a = 255;
        var offset = 0;

        if (value.Length == 8)
        {
            if (!TryParseByte(value, 0, out a))
            {
                return false;
            }

            offset = 2;
        }

        if (TryParseByte(value, offset, out var r)
            && TryParseByte(value, offset + 2, out var g)
            && TryParseByte(value, offset + 4, out var b))
        {
            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        return false;
    }

    private static bool TryParseByte(string value, int start, out byte result)
        => byte.TryParse(
            value.AsSpan(start, 2),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out result);
}

/// <summary>
/// Describes a single in-app toast.
/// </summary>
/// <param name="Title">The bold title line.</param>
/// <param name="Message">The secondary message line.</param>
/// <param name="BadgeColorHex">Optional badge background color as a hex string (e.g. <c>#RRGGBB</c>).</param>
public sealed record ToastInfo(string Title, string Message, string? BadgeColorHex = null);
