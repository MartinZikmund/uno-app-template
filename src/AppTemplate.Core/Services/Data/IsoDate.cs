using System.Globalization;

namespace AppTemplate.Core.Services.Data;

/// <summary>
/// The one place dates are turned into text for storage or export. Always invariant culture:
/// a date written under a culture with a non-Gregorian calendar (fa, ar-SA) is unreadable
/// once the user switches language.
/// </summary>
public static class IsoDate
{
    public const string Format = "yyyy-MM-dd";

    public static string ToStorage(DateOnly date) => date.ToString(Format, CultureInfo.InvariantCulture);

    public static DateOnly Parse(string value) => DateOnly.ParseExact(value, Format, CultureInfo.InvariantCulture);

    public static bool TryParse(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static string TimestampToStorage(DateTime timestamp) =>
        timestamp.ToString("O", CultureInfo.InvariantCulture);

    public static DateTime ParseTimestamp(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static bool TryParseTimestamp(string? value, out DateTime timestamp) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp);
}
