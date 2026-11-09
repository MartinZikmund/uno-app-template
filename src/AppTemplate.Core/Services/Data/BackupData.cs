using System.Text.Json.Serialization;

namespace AppTemplate.Core.Services.Data;

public static class BackupFormat
{
    /// <summary>What a file without a <c>version</c> is read as: it predates the field.</summary>
    public const int LegacyVersion = 1;

    public const int CurrentVersion = 1;

    public static bool IsSupported(int version) => version is >= LegacyVersion and <= CurrentVersion;
}

/// <summary>A full snapshot of the user's data. Dates are ISO text via <see cref="IsoDate"/>.</summary>
public sealed class BackupData
{
    /// <summary>Null for files written before versioning; read as <see cref="BackupFormat.LegacyVersion"/>.</summary>
    public int? Version { get; set; }

    public string? ExportedAt { get; set; }

    /// <summary>Null leaves the importing device's settings alone.</summary>
    public BackupSettings? Settings { get; set; }

    /// <summary>Null when the file has no <c>entries</c>; import rejects that rather than treating it as empty.</summary>
    public List<BackupEntry>? Entries { get; set; }
}

public sealed class BackupSettings
{
    /// <summary>An <c>ElementTheme</c> name.</summary>
    public string? Theme { get; set; }
}

public sealed class BackupEntry
{
    public string? Date { get; set; }

    public string? Title { get; set; }

    public double MassKilograms { get; set; }

    public string? CreatedAt { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BackupData))]
internal sealed partial class BackupJsonContext : JsonSerializerContext;
