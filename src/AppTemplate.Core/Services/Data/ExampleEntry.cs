using SQLite;

namespace AppTemplate.Core.Services.Data;

/// <summary>Placeholder model for the sample table. Replace it with your own.</summary>
public sealed partial record ExampleEntry
{
    public int Id { get; init; }

    public DateOnly Date { get; init; }

    public string Title { get; init; } = "";

    /// <summary>Canonical unit; see <see cref="MassUnits"/> for display conversion.</summary>
    public double MassKilograms { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Row shape of the sample table. Dates are ISO text, written via <see cref="IsoDate"/>.</summary>
[Table("ExampleEntries")]
internal sealed class ExampleEntryEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Date { get; set; } = "";

    public string Title { get; set; } = "";

    public double MassKilograms { get; set; }

    public string CreatedAt { get; set; } = "";
}
