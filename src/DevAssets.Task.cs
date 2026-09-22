// Compiled by RoslynCodeTaskFactory (src/DevAssets.targets) and by AppTemplate.Core.Tests. The factory has no implicit
// usings and targets netstandard2.0 under Visual Studio's MSBuild: explicit usings, netstandard2.0 APIs, no records.
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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

    static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

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

    /// <summary>Returns <paramref name="svg"/> with the badge drawn on top; the original drawing is nested unchanged.</summary>
    /// <exception cref="FormatException">The root isn't &lt;svg&gt; or has no usable viewBox, width or height.</exception>
    /// <exception cref="XmlException">The SVG isn't well-formed.</exception>
    public static string Compose(string svg, DevBadgeMask mask, double scale, out bool fits)
    {
        XElement original = Load(svg);
        double[] viewBox = ReadViewBox(original);
        double x = viewBox[0];
        double y = viewBox[1];
        double width = viewBox[2];
        double height = viewBox[3];
        BadgeBox badge = Place(x, y, width, height, mask, scale, out fits);

        XElement wrapper = new(Svg + "svg", new XAttribute("viewBox", string.Join(" ", viewBox.Select(v => Format(v)))));
        foreach (string size in new[] { "width", "height" })
        {
            if (original.Attribute(size) is { } attribute)
            {
                wrapper.SetAttributeValue(size, attribute.Value);
            }
        }

        // With an identical viewBox the original maps 1:1 and keeps its own namespaces, defs and ids.
        original.SetAttributeValue("x", Format(x));
        original.SetAttributeValue("y", Format(y));
        original.SetAttributeValue("width", Format(width));
        original.SetAttributeValue("height", Format(height));
        wrapper.Add(original);
        wrapper.Add(BadgeElement(badge, Math.Min(width, height)));
        return wrapper.ToString(SaveOptions.DisableFormatting);
    }

    static XElement BadgeElement(BadgeBox badge, double side)
    {
        double glyphScale = TextSizeRatio * side / UnitsPerEm;
        double textX = badge.X + (badge.Width - TextAdvance * glyphScale) / 2;
        double baseline = badge.Y + (badge.Height + CapHeight * glyphScale) / 2;
        return new XElement(Svg + "g",
            new XAttribute("id", "dev-badge"),
            new XElement(Svg + "rect",
                new XAttribute("x", Format(badge.X)),
                new XAttribute("y", Format(badge.Y)),
                new XAttribute("width", Format(badge.Width)),
                new XAttribute("height", Format(badge.Height)),
                new XAttribute("rx", Format(badge.Radius)),
                new XAttribute("fill", Fill)),
            new XElement(Svg + "path",
                new XAttribute("transform", $"translate({Format(textX)} {Format(baseline)}) scale({Format(glyphScale, "0.#########")})"),
                new XAttribute("fill", TextFill),
                new XAttribute("d", TextPath)));
    }

    static XElement Load(string svg)
    {
        XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
        using XmlReader reader = XmlReader.Create(new StringReader(svg), settings);
        XElement root = XDocument.Load(reader).Root ?? throw new FormatException("The SVG has no root element.");
        if (root.Name.LocalName != "svg")
        {
            throw new FormatException($"The root element is <{root.Name.LocalName}>, not <svg>.");
        }

        // Without this, a nested SVG that omits xmlns serialises as xmlns="" and renderers skip it.
        foreach (XElement element in root.DescendantsAndSelf().Where(e => e.Name.Namespace == XNamespace.None).ToList())
        {
            element.Name = Svg + element.Name.LocalName;
        }

        return root;
    }

    static double[] ReadViewBox(XElement root)
    {
        string? viewBox = (string?)root.Attribute("viewBox");
        if (!string.IsNullOrWhiteSpace(viewBox))
        {
            double[] parts = viewBox!.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(ParseNumber).ToArray();
            if (parts.Length == 4 && parts[2] > 0 && parts[3] > 0)
            {
                return parts;
            }

            throw new FormatException($"The viewBox '{viewBox}' is not four numbers with a positive size.");
        }

        return [0, 0, ReadLength(root, "width"), ReadLength(root, "height")];
    }

    static double ReadLength(XElement root, string name)
    {
        string raw = ((string?)root.Attribute(name) ?? "").Trim();
        if (raw.EndsWith("px", StringComparison.Ordinal))
        {
            raw = raw.Substring(0, raw.Length - 2);
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value > 0)
        {
            return value;
        }

        throw new FormatException($"The SVG has no viewBox and its {name} '{raw}' is not a plain number.");
    }

    static double ParseNumber(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    static string Format(double value, string pattern = "0.####") => value.ToString(pattern, CultureInfo.InvariantCulture);

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

/// <summary>Which mask and scale an asset is rendered with, resolved the way Uno.Resizetizer reads them.</summary>
public sealed class DevAssetPlacement(DevBadgeMask mask, double scale)
{
    public DevBadgeMask Mask { get; } = mask;

    public double Scale { get; } = scale;

    public static DevAssetPlacement Resolve(bool isIcon, string targetPlatform, ITaskItem asset)
    {
        string platform = targetPlatform.ToLowerInvariant();
        string metadata = isIcon ? "ForegroundScale" : "Scale";

        // A platform override (e.g. AndroidForegroundScale) wins, as in Resizetizer's ResizeImageInfo.
        double scale = ReadScale(asset, PlatformPrefix(platform) + metadata) ?? ReadScale(asset, metadata) ?? 1.0;
        DevBadgeMask mask = (platform, isIcon) switch
        {
            ("android", true) => DevBadgeMask.AndroidAdaptive,
            ("android", false) => DevBadgeMask.AndroidSplash,
            ("ios", true) => DevBadgeMask.IosIcon,
            _ => DevBadgeMask.None,
        };
        return new DevAssetPlacement(mask, scale);
    }

    static string PlatformPrefix(string platform) => platform switch
    {
        "android" => "Android",
        "ios" => "IOS",
        "windows" => "Windows",
        "browserwasm" => "Wasm",
        _ => "Skia",
    };

    static double? ReadScale(ITaskItem asset, string name) =>
        double.TryParse(asset.GetMetadata(name), NumberStyles.Number, CultureInfo.InvariantCulture, out double value) && value > 0 ? value : null;
}

// The base type is fully qualified: the test project's implicit usings also bring System.Threading.Tasks.Task into scope.

/// <summary>
/// Replaces each UnoIcon foreground (Kind=Icon) or UnoSplashScreen image (Kind=Splash) with a badged copy at
/// OutputRoot/&lt;content hash&gt;/&lt;original file name&gt;, so every generated resource name stays the same.
/// </summary>
public sealed class ComposeDevAsset : Microsoft.Build.Utilities.Task
{
    static readonly UTF8Encoding Utf8 = new(false);

    [Required]
    public ITaskItem[] Assets { get; set; } = [];

    /// <summary>"Icon" rewrites ForegroundFile; "Splash" rewrites the item spec.</summary>
    [Required]
    public string Kind { get; set; } = "";

    public string TargetPlatform { get; set; } = "";

    [Required]
    public string ProjectDirectory { get; set; } = "";

    [Required]
    public string OutputRoot { get; set; } = "";

    [Output]
    public ITaskItem[] Result { get; set; } = [];

    public override bool Execute()
    {
        bool isIcon = string.Equals(Kind, "Icon", StringComparison.OrdinalIgnoreCase);
        string root = Path.GetFullPath(Path.Combine(ProjectDirectory, OutputRoot));
        Result = Assets.Select(asset => Compose(asset, isIcon, root)).ToArray();
        return !Log.HasLoggedErrors;
    }

    ITaskItem Compose(ITaskItem asset, bool isIcon, string root)
    {
        string file = isIcon ? asset.GetMetadata("ForegroundFile") : asset.ItemSpec;
        if (file.Length == 0)
        {
            return asset;
        }

        string source = Path.GetFullPath(Path.Combine(ProjectDirectory, file));
        if (source.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return asset;
        }

        DevAssetPlacement placement = DevAssetPlacement.Resolve(isIcon, TargetPlatform, asset);
        string composed;
        bool fits;
        try
        {
            composed = DevBadge.Compose(File.ReadAllText(source), placement.Mask, placement.Scale, out fits);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException or FormatException)
        {
            Log.LogWarning(null, "DEVASSETS001", null, source, 0, 0, 0, 0, "Dev badge skipped, the original artwork is used instead: {0}", ex.Message);
            return asset;
        }

        if (!fits)
        {
            Log.LogWarning(null, "DEVASSETS002", null, source, 0, 0, 0, 0, "The Dev badge doesn't fit inside the {0} mask at scale {1} and may be clipped.", placement.Mask, placement.Scale.ToString(CultureInfo.InvariantCulture));
        }

        string badged = Publish(composed, root, Path.GetFileName(source));
        if (isIcon)
        {
            TaskItem icon = new(asset);
            icon.SetMetadata("ForegroundFile", badged);
            return icon;
        }

        return new TaskItem(badged, asset.CloneCustomMetadata());
    }

    static string Publish(string content, string root, string fileName)
    {
        byte[] bytes = Utf8.GetBytes(content);
        string hash;
        using (SHA256 sha = SHA256.Create())
        {
            hash = string.Concat(sha.ComputeHash(bytes).Take(6).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        string directory = Path.Combine(root, hash);
        string path = Path.Combine(directory, fileName);
        if (File.Exists(path))
        {
            return path;
        }

        // Android's inner builds compose the same asset from several project instances at once, so publish atomically.
        Directory.CreateDirectory(directory);
        string temp = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllBytes(temp, bytes);
        try
        {
            File.Move(temp, path);
        }
        catch (IOException)
        {
            // Another instance won the race with identical content.
            File.Delete(temp);
        }

        return path;
    }
}
