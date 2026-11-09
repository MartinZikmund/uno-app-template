using SQLite;

namespace AppTemplate.Core.Tests.Services.Data;

/// <summary>A throwaway database file under the temp folder, plus raw access for inspecting it.</summary>
internal sealed class TempDatabase : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "AppTemplate.Tests", $"{Guid.NewGuid():N}.db");

    public int ReadUserVersion() => Query(c => c.ExecuteScalar<int>("PRAGMA user_version"));

    public void SetUserVersion(int version) => Query(c => c.Execute($"PRAGMA user_version = {version}"));

    public bool TableExists(string table) => Query(c => c.ExecuteScalar<int>(
        "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", table) > 0);

    public bool ColumnExists(string table, string column) =>
        Query(c => c.GetTableInfo(table).Any(col => col.Name == column));

    public T Query<T>(Func<SQLiteConnection, T> query)
    {
        using SQLiteConnection connection = new(Path);
        return query(connection);
    }

    public void Dispose()
    {
        try
        {
            File.Delete(Path);
        }
        catch (IOException)
        {
            // A pooled handle can outlive the test briefly; the temp folder is fine to leave behind.
        }
    }
}
