namespace AppTemplate.Core.Services.Data;

public interface IBackupService
{
    /// <summary>Serializes every entry plus the settings worth carrying over.</summary>
    Task<string> ExportAsync();

    /// <summary>
    /// Replaces the current data with the backup. All-or-nothing: the whole file is validated
    /// before anything is written, and a failure part way through rolls everything back.
    /// </summary>
    Task<BackupImportResult> ImportAsync(string json);
}

public enum BackupImportError
{
    None,
    InvalidFormat,
    UnsupportedVersion,
    InvalidData,
    WriteFailed,
}

/// <summary>Errors are codes rather than text, so the UI can localize them.</summary>
public sealed record BackupImportResult(BackupImportError Error, int EntriesImported = 0)
{
    public bool Success => Error == BackupImportError.None;

    public static BackupImportResult Succeeded(int entriesImported) => new(BackupImportError.None, entriesImported);
}
