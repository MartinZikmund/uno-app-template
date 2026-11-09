using SQLite;

namespace AppTemplate.Core.Services.Data;

/// <summary>One schema step. <see cref="Apply"/> runs inside the same transaction as the version bump.</summary>
public sealed record SchemaMigration(int Version, Action<SQLiteConnection> Apply);

/// <summary>
/// The schema history, keyed on <c>PRAGMA user_version</c>. Append new steps; never edit
/// one that has shipped, since existing databases already ran it.
/// </summary>
public static class SchemaMigrations
{
    public static IReadOnlyList<SchemaMigration> All { get; } =
    [
        new(1, c => c.Execute("""
            CREATE TABLE ExampleEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Date TEXT NOT NULL,
                Title TEXT NOT NULL,
                MassKilograms REAL NOT NULL,
                CreatedAt TEXT NOT NULL
            )
            """)),
    ];

    public static int LatestVersion => All[^1].Version;
}
