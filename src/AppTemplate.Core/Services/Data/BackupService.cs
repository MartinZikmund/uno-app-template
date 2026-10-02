using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AppTemplate.Services.Settings;

namespace AppTemplate.Core.Services.Data;

public sealed class BackupService(IDataService dataService, IAppPreferences preferences) : IBackupService
{
    public async Task<string> ExportAsync()
    {
        IReadOnlyList<ExampleEntry> entries = await dataService.GetEntriesAsync();

        BackupData backup = new()
        {
            Version = BackupFormat.CurrentVersion,
            ExportedAt = IsoDate.TimestampToStorage(DateTime.UtcNow),
            Settings = CaptureSettings(),
            Entries =
            [
                .. entries.Select(e => new BackupEntry
                {
                    Date = IsoDate.ToStorage(e.Date),
                    Title = e.Title,
                    MassKilograms = e.MassKilograms,
                    CreatedAt = IsoDate.TimestampToStorage(e.CreatedAt),
                }),
            ],
        };

        return JsonSerializer.Serialize(backup, BackupJsonContext.Default.BackupData);
    }

    public async Task<BackupImportResult> ImportAsync(string json)
    {
        BackupData? backup;
        try
        {
            backup = JsonSerializer.Deserialize(json, BackupJsonContext.Default.BackupData);
        }
        catch (JsonException)
        {
            return new(BackupImportError.InvalidFormat);
        }

        if (backup?.Entries is null)
        {
            return new(BackupImportError.InvalidFormat);
        }

        if (!BackupFormat.IsSupported(backup.Version ?? BackupFormat.LegacyVersion))
        {
            return new(BackupImportError.UnsupportedVersion);
        }

        // Validate everything up front: import replaces the existing data, so a bad row found
        // half way through would otherwise mean a rollback at best and a half-restore at worst.
        ElementTheme? theme = null;
        if (backup.Settings?.Theme is { } themeName && !TryParseTheme(themeName, out theme))
        {
            return new(BackupImportError.InvalidData);
        }

        List<ExampleEntry> entries = new(backup.Entries.Count);
        foreach (BackupEntry row in backup.Entries)
        {
            if (!TryParseEntry(row, out ExampleEntry? entry))
            {
                return new(BackupImportError.InvalidData);
            }

            entries.Add(entry);
        }

        // Settings go first, so anything that reads them while the data lands sees the backup's.
        // They live outside the database transaction, so a failure puts them back by hand.
        BackupSettings previousSettings = CaptureSettings();
        try
        {
            if (theme is { } newTheme)
            {
                preferences.Theme = newTheme;
            }

            await dataService.RunInTransactionAsync(async () =>
            {
                await dataService.DeleteAllEntriesAsync();
                foreach (ExampleEntry entry in entries)
                {
                    await dataService.SaveEntryAsync(entry);
                }
            });
        }
        catch (Exception)
        {
            ApplySettings(previousSettings);
            return new(BackupImportError.WriteFailed);
        }

        return BackupImportResult.Succeeded(entries.Count);
    }

    private BackupSettings CaptureSettings() => new() { Theme = preferences.Theme.ToString() };

    private void ApplySettings(BackupSettings settings)
    {
        if (settings.Theme is { } name && TryParseTheme(name, out ElementTheme? theme))
        {
            preferences.Theme = theme.Value;
        }
    }

    private static bool TryParseTheme(string name, [NotNullWhen(true)] out ElementTheme? theme)
    {
        theme = Enum.TryParse(name, out ElementTheme parsed) && Enum.IsDefined(parsed) ? parsed : null;
        return theme is not null;
    }

    private static bool TryParseEntry(BackupEntry row, [NotNullWhen(true)] out ExampleEntry? entry)
    {
        entry = null;
        if (!IsoDate.TryParse(row.Date, out DateOnly date)
            || !ExampleEntryRules.IsValid(row.Title, row.MassKilograms)
            || !IsoDate.TryParseTimestamp(row.CreatedAt, out DateTime createdAt))
        {
            return false;
        }

        entry = new() { Date = date, Title = row.Title!, MassKilograms = row.MassKilograms, CreatedAt = createdAt };
        return true;
    }
}
