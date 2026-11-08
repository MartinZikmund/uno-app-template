#:package Svg.Skia@3.0.6

using SkiaSharp;
using Svg.Skia;

// Renders the Windows package images (app icons, tiles, splash, package logo) from one SVG,
// with the Visual Studio asset generator's names, sizes and art-to-canvas ratios.
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet run export-windows-assets.cs -- <art.svg> <output folder>");
    return 1;
}

string svgPath = args[0];
string outDir = args[1];
Directory.CreateDirectory(outDir);

using SKSvg svg = new();
SKPicture? picture = svg.Load(svgPath);
if (picture is null)
{
    Console.Error.WriteLine($"Could not load {svgPath}");
    return 1;
}

SKRect viewBox = picture.CullRect;
(int Scale, double Factor)[] scales = [(100, 1.0), (125, 1.25), (150, 1.5), (200, 2.0), (400, 4.0)];
int[] targetSizes = [16, 24, 32, 48, 256];

List<(string Name, int Width, int Height, float Art)> assets = [];

void Scales(string name, int width, int height, float art)
{
    foreach ((int scale, double factor) in scales)
    {
        assets.Add(($"{name}.scale-{scale}.png", Round(width * factor), Round(height * factor), art));
    }
}

void TargetSizes(string name, float art)
{
    foreach (int size in targetSizes)
    {
        assets.Add(($"{name}{size}.png", size, size, art));
    }
}

static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

Scales("AppIcon", 44, 44, .75f);
TargetSizes("AppIcon.targetsize-", .75f);
TargetSizes("AppIcon.altform-unplated_targetsize-", 1f);
TargetSizes("AppIcon.altform-lightunplated_targetsize-", 1f);
Scales("SmallTile", 71, 71, .5f);
Scales("MediumTile", 150, 150, .33f);
Scales("LargeTile", 310, 310, .33f);
Scales("WideTile", 310, 150, .33f);
Scales("SplashScreen", 620, 300, .33f);
Scales("PackageLogo", 50, 50, 1f);

foreach ((string name, int width, int height, float art) in assets)
{
    SKImageInfo info = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
    using SKSurface surface = SKSurface.Create(info);
    SKCanvas canvas = surface.Canvas;
    canvas.Clear(SKColors.Transparent);

    // Fit the art's longer side into the art box and centre it, so a non-square viewBox stays centred too.
    float side = Math.Min(width, height) * art;
    float scale = side / Math.Max(viewBox.Width, viewBox.Height);
    canvas.Translate((width - viewBox.Width * scale) / 2f, (height - viewBox.Height * scale) / 2f);
    canvas.Scale(scale);
    canvas.Translate(-viewBox.Left, -viewBox.Top);
    canvas.DrawPicture(picture);

    using SKImage image = surface.Snapshot();
    using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
    File.WriteAllBytes(Path.Combine(outDir, name), png.ToArray());
}

Console.WriteLine($"{assets.Count} images written to {outDir}");
return 0;
