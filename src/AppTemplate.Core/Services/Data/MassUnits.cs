namespace AppTemplate.Core.Services.Data;

/// <summary>
/// Storage is always kilograms. Convert only where a value meets the user (display and input),
/// so a change of unit preference never rewrites stored data.
/// </summary>
public static class MassUnits
{
    public const double PoundsPerKilogram = 2.20462262185;

    public static double ToDisplay(double kilograms, bool useMetric) =>
        useMetric ? kilograms : kilograms * PoundsPerKilogram;

    public static double FromDisplay(double displayed, bool useMetric) =>
        useMetric ? displayed : displayed / PoundsPerKilogram;
}
