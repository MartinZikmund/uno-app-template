using System.Globalization;
using AppTemplate.Core.Services.Data;
using FluentAssertions;

namespace AppTemplate.Core.Tests.Services.Data;

[TestClass]
public class IsoDateTests
{
    private static T UnderCulture<T>(string culture, Func<T> action)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    [DataRow("en-US")]
    [DataRow("cs-CZ")]
    [DataRow("fa-IR")] // Non-Gregorian default calendar.
    [DataRow("ar-SA")]
    public void ToStorage_UnderAnyCulture_WritesIsoDate(string culture) =>
        UnderCulture(culture, () => IsoDate.ToStorage(new DateOnly(2026, 3, 9))).Should().Be("2026-03-09");

    [TestMethod]
    public void Parse_WrittenUnderOneCultureAndReadUnderAnother_RoundTrips()
    {
        string stored = UnderCulture("fa-IR", () => IsoDate.ToStorage(new DateOnly(2026, 12, 31)));

        UnderCulture("en-US", () => IsoDate.Parse(stored)).Should().Be(new DateOnly(2026, 12, 31));
    }

    [TestMethod]
    public void TimestampToStorage_RoundTripsUtcKind()
    {
        DateTime timestamp = new(2026, 5, 1, 13, 45, 7, 123, DateTimeKind.Utc);
        string stored = UnderCulture("ar-SA", () => IsoDate.TimestampToStorage(timestamp));

        IsoDate.ParseTimestamp(stored).Should().Be(timestamp);
        IsoDate.ParseTimestamp(stored).Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("31/12/2026")]
    [DataRow("2026-13-01")]
    public void TryParse_Garbage_ReturnsFalse(string? value) =>
        IsoDate.TryParse(value, out _).Should().BeFalse();
}
