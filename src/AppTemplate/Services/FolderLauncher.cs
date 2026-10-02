namespace AppTemplate.Services;

public sealed class FolderLauncher(ILogger<FolderLauncher> logger) : IFolderLauncher
{
    public bool IsSupported =>
        OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    public Task<bool> OpenAsync(string path)
    {
        if (!IsSupported)
        {
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            logger.LogWarning("Not opening a folder that does not exist: {Path}", path);
            return Task.FromResult(false);
        }

        return OpenExistingAsync(path);
    }

#if HAS_UNO
    // Launcher.LaunchFolderPathAsync isn't implemented on Skia desktop; the OS shell is.
    private Task<bool> OpenExistingAsync(string path)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not open folder {Path}", path);
            return Task.FromResult(false);
        }
    }
#else
    private async Task<bool> OpenExistingAsync(string path)
    {
        try
        {
            return await Windows.System.Launcher.LaunchFolderPathAsync(path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not open folder {Path}", path);
            return false;
        }
    }
#endif
}
