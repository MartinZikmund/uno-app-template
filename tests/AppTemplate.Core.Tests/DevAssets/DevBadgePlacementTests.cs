using AppTemplate.Build;
using FluentAssertions;

namespace AppTemplate.Core.Tests.DevAssets;

[TestClass]
public sealed class DevBadgePlacementTests
{
    [TestMethod]
    public void Place_NoMask_IsFlushTopRight()
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.None, 1, out bool fits);

        fits.Should().BeTrue();
        badge.Right.Should().BeApproximately(100, 1e-9);
        badge.Y.Should().Be(0);
        badge.Height.Should().BeApproximately(24, 1e-9);
        badge.Radius.Should().BeApproximately(5.52, 1e-9);
    }

    [TestMethod]
    public void Place_NoMask_WidthIsTextAdvancePlusPadding()
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.None, 1, out _);

        // 13.92 * 3892 / 2048 (the "DEV" advance) + 2 * 8.5
        badge.Width.Should().BeApproximately(43.4534, 1e-3);
    }

    [TestMethod]
    public void Place_OffsetViewBox_AnchorsToViewBoxCorner()
    {
        BadgeBox badge = DevBadge.Place(-10, -10, 120, 120, DevBadgeMask.None, 1, out _);

        badge.Right.Should().BeApproximately(110, 1e-9);
        badge.Y.Should().Be(-10);
        badge.Height.Should().BeApproximately(28.8, 1e-9);
    }

    [TestMethod]
    public void Place_NonSquareViewBox_SizesFromShorterSide()
    {
        BadgeBox badge = DevBadge.Place(0, 0, 200, 100, DevBadgeMask.None, 1, out _);

        badge.Height.Should().BeApproximately(24, 1e-9);
        badge.Right.Should().BeApproximately(200, 1e-9);
    }

    [TestMethod]
    [DataRow(0.6)]
    [DataRow(0.65)]
    [DataRow(0.8)]
    public void Place_AndroidAdaptive_StaysInsideSafeZone(double scale)
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.AndroidAdaptive, scale, out bool fits);

        fits.Should().BeTrue();
        FarthestReach(badge).Should().BeLessThanOrEqualTo(100 * (33.0 / 108.0) / scale);
    }

    [TestMethod]
    [DataRow(0.85)]
    [DataRow(1.0)]
    public void Place_AndroidSplash_StaysInsideSplashCircle(double scale)
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.AndroidSplash, scale, out bool fits);

        fits.Should().BeTrue();
        FarthestReach(badge).Should().BeLessThanOrEqualTo(100 * (36.0 / 108.0) / scale);
    }

    [TestMethod]
    [DataRow(1.0)]
    [DataRow(0.8)]
    public void Place_IosIcon_ClearsRoundedCorner(double scale)
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.IosIcon, scale, out bool fits);

        fits.Should().BeTrue();
        double half = 50 / scale;
        double corner = 22.37 / scale;
        double arcX = 50 + half - corner;
        double arcY = 50 - half + corner;
        double discX = badge.Right - badge.Radius;
        double discY = badge.Y + badge.Radius;
        if (discX > arcX && discY < arcY)
        {
            (Distance(discX - arcX, discY - arcY) + badge.Radius).Should().BeLessThanOrEqualTo(corner);
        }

        badge.Right.Should().BeLessThanOrEqualTo(50 + half);
        badge.Y.Should().BeGreaterThanOrEqualTo(50 - half);
    }

    [TestMethod]
    [DataRow(DevBadgeMask.AndroidAdaptive, 0.6)]
    [DataRow(DevBadgeMask.AndroidSplash, 0.85)]
    [DataRow(DevBadgeMask.IosIcon, 1.0)]
    public void Place_MaskClipsFlushBadge_PullsDiagonallyTowardsCentre(DevBadgeMask mask, double scale)
    {
        BadgeBox flush = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.None, 1, out _);

        BadgeBox pulled = DevBadge.Place(0, 0, 100, 100, mask, scale, out _);

        double dx = pulled.X - flush.X;
        double dy = pulled.Y - flush.Y;
        dx.Should().BeNegative();
        dy.Should().BeApproximately(-dx, 1e-9);
    }

    [TestMethod]
    public void Place_MaskSmallerThanBadge_ReportsNotFitting()
    {
        DevBadge.Place(0, 0, 100, 100, DevBadgeMask.AndroidAdaptive, 3.0, out bool fits);

        fits.Should().BeFalse();
    }

    [TestMethod]
    public void IsVisible_NoMask_IsAlwaysTrue()
    {
        DevBadge.IsVisible(new BadgeBox(-1, -1, 3, 3, 0), DevBadgeMask.None, 1).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(DevBadgeMask.AndroidAdaptive)]
    [DataRow(DevBadgeMask.AndroidSplash)]
    [DataRow(DevBadgeMask.IosIcon)]
    public void IsVisible_SmallBadgeAtCentre_IsTrue(DevBadgeMask mask)
    {
        DevBadge.IsVisible(new BadgeBox(0.45, 0.45, 0.1, 0.1, 0.01), mask, 1).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(DevBadgeMask.AndroidAdaptive)]
    [DataRow(DevBadgeMask.AndroidSplash)]
    [DataRow(DevBadgeMask.IosIcon)]
    public void IsVisible_SquareBadgeInCorner_IsFalse(DevBadgeMask mask)
    {
        DevBadge.IsVisible(new BadgeBox(0.9, 0, 0.1, 0.1, 0), mask, 1).Should().BeFalse();
    }

    // Farthest point of the rounded badge from the image centre: a corner disc's centre distance plus its radius.
    static double FarthestReach(BadgeBox b) => new[]
    {
        (X: b.X + b.Radius, Y: b.Y + b.Radius),
        (X: b.Right - b.Radius, Y: b.Y + b.Radius),
        (X: b.X + b.Radius, Y: b.Bottom - b.Radius),
        (X: b.Right - b.Radius, Y: b.Bottom - b.Radius),
    }.Max(c => Distance(c.X - 50, c.Y - 50) + b.Radius);

    static double Distance(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);
}
