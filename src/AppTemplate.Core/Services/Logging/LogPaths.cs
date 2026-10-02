namespace AppTemplate.Services.Logging;

/// <summary>
/// Where the file log lives and how big it may grow. The file rolls on size, never on date,
/// so its name carries no timestamp and the folder shown to the user stays stable.
/// </summary>
public static class LogPaths
{
    public const string FolderName = "Logs";

    public const string FileName = "app.log";

    public const long FileSizeLimitBytes = 5 * 1024 * 1024;

    public const int RetainedFileCountLimit = 5;

    public static string GetFolder(string appDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataPath);
        return Path.Combine(appDataPath, FolderName);
    }

    public static string GetFilePath(string logFolder) => Path.Combine(logFolder, FileName);
}
