namespace AppTemplate.Services.Logging;

public interface ILogFolderProvider
{
    string FolderPath { get; }
}

public sealed class LogFolderProvider(string folderPath) : ILogFolderProvider
{
    public string FolderPath { get; } = folderPath;
}
