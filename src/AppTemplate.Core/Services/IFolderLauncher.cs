namespace AppTemplate.Services;

/// <summary>Opens a folder in the system file manager.</summary>
public interface IFolderLauncher
{
    /// <summary>
    /// Whether this platform has a file manager to open. Android, iOS and WebAssembly keep app
    /// data private (or have no file system), so gate the UI on this.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>Returns <see langword="false"/> when the folder could not be opened; never throws.</summary>
    Task<bool> OpenAsync(string path);
}
