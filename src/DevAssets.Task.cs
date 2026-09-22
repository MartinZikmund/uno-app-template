// Compiled by RoslynCodeTaskFactory (src/DevAssets.targets) and by AppTemplate.Core.Tests. The factory has no implicit
// usings and targets netstandard2.0 under Visual Studio's MSBuild: explicit usings, netstandard2.0 APIs, no records.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace AppTemplate.Build;

/// <summary>The platform mask an image is shown through.</summary>
public enum DevBadgeMask
{
    None,
    AndroidAdaptive,
    AndroidSplash,
    IosIcon,
}

/// <summary>A rounded rectangle.</summary>
public sealed class BadgeBox(double x, double y, double width, double height, double radius)
{
    public double X { get; } = x;

    public double Y { get; } = y;

    public double Width { get; } = width;

    public double Height { get; } = height;

    public double Radius { get; } = radius;

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public BadgeBox Offset(double dx, double dy) => new(X + dx, Y + dy, Width, Height, Radius);

    /// <summary>Maps the box into a unit square whose top-left corner is (<paramref name="originX"/>, <paramref name="originY"/>).</summary>
    public BadgeBox Normalize(double originX, double originY, double size) =>
        new((X - originX) / size, (Y - originY) / size, Width / size, Height / size, Radius / size);
}

/// <summary>Stamps the Dev-channel badge onto an SVG.</summary>
public static class DevBadge
{
    // The in-app DevChannelBadge (Padding 6,2, 10px SemiBold text, CornerRadius 4, ~17.3px tall) scaled to 24% of
    // the image. Fractions of the image's shorter side.
    public const double HeightRatio = 0.24;
    public const double CornerRadiusRatio = 0.0552;
    public const double PaddingRatio = 0.085;
    public const double TextSizeRatio = 0.1392;
    public const string Fill = "#FFB900";
    public const string TextFill = "#141414";

    // Visible regions in frame units: the square the image is fitted into, before ForegroundScale / Scale.
    const double AndroidAdaptiveSafeRadius = 33.0 / 108.0; // 66dp safe zone of the 108dp adaptive layer
    const double AndroidSplashRadius = 36.0 / 108.0;       // 72dp visible circle of the 108dp Android 12+ splash drawable
    const double IosCornerRatio = 0.2237;
    const double Margin = 0.01;
    const double PullStep = 0.0025;
    const int MaxPullSteps = 200;

    // "DEV" in Selawik Semibold 1.01 (© Microsoft, SIL Open Font License 1.1), as outlines so the badge never depends
    // on the build machine's fonts. Font units, baseline at y = 0, y pointing down.
    const double UnitsPerEm = 2048;
    const double CapHeight = 1434;
    const double TextAdvance = 3892;
    const string TextPath = "M172 -1433H671Q1034 -1433 1219 -1261Q1404 -1089 1404 -734Q1404 -510 1311 -344Q1218 -179 1048 -90Q879 0 656 0L172 2ZM658 -225Q808 -225 914 -284Q1021 -343 1076 -456Q1132 -568 1132 -728Q1132 -971 1014 -1090Q897 -1210 667 -1209L437 -1207V-225ZM1657 -1434H2452V-1215H1922V-837H2390V-616H1922V-219H2484V0H1657ZM2569 -1433H2857L3191 -380Q3215 -308 3221 -253H3225Q3229 -281 3238 -316Q3248 -350 3259 -383L3603 -1434L3881 -1433L3373 1H3070Z";

    /// <summary>
    /// Returns the badge in viewBox units: flush top-right, then pulled towards the centre until the mask shows all of it.
    /// </summary>
    public static BadgeBox Place(double viewBoxX, double viewBoxY, double viewBoxWidth, double viewBoxHeight, DevBadgeMask mask, double scale, out bool fits)
    {
        double side = Math.Min(viewBoxWidth, viewBoxHeight);
        double width = (TextSizeRatio * TextAdvance / UnitsPerEm + 2 * PaddingRatio) * side;
        BadgeBox flush = new(viewBoxX + viewBoxWidth - width, viewBoxY, width, HeightRatio * side, CornerRadiusRatio * side);

        // Resizetizer fits the image into a square canvas and scales it about the centre, so masks live on that square.
        double frame = Math.Max(viewBoxWidth, viewBoxHeight);
        double frameX = viewBoxX + (viewBoxWidth - frame) / 2;
        double frameY = viewBoxY + (viewBoxHeight - frame) / 2;

        // Diagonal pull keeps the badge in its corner as far as the mask allows.
        double dx = Math.Sign(Math.Round(frameX + frame / 2 - (flush.X + flush.Right) / 2, 9));
        double dy = Math.Sign(Math.Round(frameY + frame / 2 - (flush.Y + flush.Bottom) / 2, 9));
        double length = Math.Sqrt(dx * dx + dy * dy);

        BadgeBox badge = flush;
        for (int step = 0; step <= MaxPullSteps; step++)
        {
            double distance = length == 0 ? 0 : step * PullStep * frame / length;
            badge = flush.Offset(dx * distance, dy * distance);
            if (IsVisible(badge.Normalize(frameX, frameY, frame), mask, scale))
            {
                fits = true;
                return badge;
            }
        }

        fits = false;
        return badge;
    }

    /// <summary>Whether a badge, in frame units (0..1), is fully visible through the mask.</summary>
    public static bool IsVisible(BadgeBox badge, DevBadgeMask mask, double scale) => mask switch
    {
        DevBadgeMask.AndroidAdaptive => InsideCircle(badge, AndroidAdaptiveSafeRadius / scale - Margin),
        DevBadgeMask.AndroidSplash => InsideCircle(badge, AndroidSplashRadius / scale - Margin),
        DevBadgeMask.IosIcon => InsideRoundedSquare(badge, 0.5 / scale - Margin, Math.Max(0, IosCornerRatio / scale - Margin)),
        _ => true,
    };

    // Every mask is convex, so the rounded badge is inside it exactly when its four corner discs are.
    static bool InsideCircle(BadgeBox badge, double radius) =>
        CornerCentres(badge).All(c => Distance(c.X - 0.5, c.Y - 0.5) + badge.Radius <= radius);

    static bool InsideRoundedSquare(BadgeBox badge, double halfSide, double cornerRadius)
    {
        // A disc fits a rounded square when its centre fits the square shrunk by the disc's radius.
        double half = halfSide - badge.Radius;
        double corner = Math.Max(0, cornerRadius - badge.Radius);
        double straight = half - corner;
        return CornerCentres(badge).All(c =>
        {
            double dx = Math.Abs(c.X - 0.5);
            double dy = Math.Abs(c.Y - 0.5);
            return dx <= half && dy <= half && (dx <= straight || dy <= straight || Distance(dx - straight, dy - straight) <= corner);
        });
    }

    static IEnumerable<(double X, double Y)> CornerCentres(BadgeBox badge)
    {
        yield return (badge.X + badge.Radius, badge.Y + badge.Radius);
        yield return (badge.Right - badge.Radius, badge.Y + badge.Radius);
        yield return (badge.X + badge.Radius, badge.Bottom - badge.Radius);
        yield return (badge.Right - badge.Radius, badge.Bottom - badge.Radius);
    }

    static double Distance(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);
}
