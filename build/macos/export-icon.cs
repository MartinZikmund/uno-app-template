#:package Svg.Skia@3.0.6

using SkiaSharp;
using Svg.Skia;

// Renders the macOS app icon sizes on Apple's icon grid: an 824 px rounded plate with a soft shadow on a 1024 px canvas.
// Layers work like the project's UnoIcon: a background (an SVG stretched over the plate, or a colour) and an optional
// foreground SVG, centred and scaled like UnoIconForegroundScale.
if (args.Length is < 2 or > 4)
{
    Console.Error.WriteLine("Usage: dotnet run export-icon.cs -- <output folder> <background .svg or #RRGGBB> [foreground .svg] [foreground scale, default 1]");
    return 1;
}

string outDir = args[0];
string background = args[1];
string? foregroundPath = args.Length > 2 ? args[2] : null;
float foregroundScale = args.Length > 3 ? float.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture) : 1f;
Directory.CreateDirectory(outDir);

SKPicture? backgroundPicture = background.StartsWith('#') ? null : Load(background);
SKColor backgroundColor = background.StartsWith('#') ? SKColor.Parse(background) : SKColors.Transparent;
SKPicture? foregroundPicture = foregroundPath is null ? null : Load(foregroundPath);

const float Canvas = 1024f, Plate = 824f, Corner = 185f;

int written = 0;
foreach (int points in new[] { 16, 32, 128, 256, 512 })
{
    foreach (int scale in new[] { 1, 2 })
    {
        int size = points * scale;
        SKImageInfo info = new(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(size / Canvas);

        SKRect plate = SKRect.Create((Canvas - Plate) / 2f, (Canvas - Plate) / 2f, Plate, Plate);
        using (SKPaint shadow = new() { IsAntialias = true, Color = new SKColor(0, 0, 0, 76), ImageFilter = SKImageFilter.CreateBlur(14, 14) })
        {
            canvas.DrawRoundRect(SKRect.Create(plate.Left, plate.Top + 10, Plate, Plate), Corner, Corner, shadow);
        }

        canvas.ClipRoundRect(new SKRoundRect(plate, Corner), antialias: true);
        using (SKPaint fill = new() { IsAntialias = true, Color = backgroundColor })
        {
            canvas.DrawRect(plate, fill);
        }

        if (backgroundPicture is not null)
        {
            DrawCentered(canvas, backgroundPicture, Plate, stretch: true);
        }

        if (foregroundPicture is not null)
        {
            DrawCentered(canvas, foregroundPicture, Plate * foregroundScale, stretch: false);
        }

        using SKImage image = surface.Snapshot();
        using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(outDir, $"icon{points}x{points}@{scale}x.png"), png.ToArray());
        written++;
    }
}

Console.WriteLine($"{written} images written to {outDir}");
return 0;

static SKPicture Load(string path)
{
    SKSvg svg = new();
    return svg.Load(path) ?? throw new InvalidOperationException($"Could not load {path}");
}

static void DrawCentered(SKCanvas canvas, SKPicture picture, float side, bool stretch)
{
    SKRect box = picture.CullRect;
    canvas.Save();
    canvas.Translate((Canvas - side) / 2f, (Canvas - side) / 2f);
    if (stretch)
    {
        canvas.Scale(side / box.Width, side / box.Height);
    }
    else
    {
        float fit = side / Math.Max(box.Width, box.Height);
        canvas.Translate((side - box.Width * fit) / 2f, (side - box.Height * fit) / 2f);
        canvas.Scale(fit);
    }

    canvas.Translate(-box.Left, -box.Top);
    canvas.DrawPicture(picture);
    canvas.Restore();
}
