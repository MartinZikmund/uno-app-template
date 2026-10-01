using SQLite;

namespace AppTemplate.Core.Services.Data;

public sealed class SqliteDataService : IDataService, IAsyncDisposable
{
    private readonly SQLiteAsyncConnection _connection;
    private readonly IReadOnlyList<SchemaMigration> _migrations;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private bool _initialized;

    // sqlite-net locks per statement, not per transaction, so without this gate an unrelated
    // write lands between BEGIN and COMMIT and is thrown away by a rollback it has nothing to do with.
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    // Set on the flow running a transaction body, whose writes belong inside it.
    private readonly AsyncLocal<bool> _inTransaction = new();

    /// <param name="databasePath">Database file; its folder is created if missing.</param>
    /// <param name="migrations">Schema history; defaults to <see cref="SchemaMigrations.All"/>.</param>
    public SqliteDataService(string databasePath, IReadOnlyList<SchemaMigration>? migrations = null)
    {
        string? directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connection = new(databasePath);
        _migrations = migrations ?? SchemaMigrations.All;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initGate.WaitAsync();
        try
        {
            if (!_initialized)
            {
                await MigrateAsync();
                _initialized = true;
            }
        }
        finally
        {
            _initGate.Release();
        }
    }

    private async Task MigrateAsync()
    {
        int version = await _connection.ExecuteScalarAsync<int>("PRAGMA user_version");
        int latest = _migrations.Count == 0 ? 0 : _migrations.Max(m => m.Version);
        if (version > latest)
        {
            throw new InvalidOperationException(
                $"Database schema version {version} is newer than this app understands ({latest}).");
        }

        foreach (SchemaMigration migration in _migrations.Where(m => m.Version > version).OrderBy(m => m.Version))
        {
            // The step and its version bump commit together (user_version lives in the database
            // header, so it rolls back too). Otherwise a kill in between leaves the work done but
            // unrecorded, and the next launch runs it a second time.
            await _connection.RunInTransactionAsync(c =>
            {
                migration.Apply(c);
                c.Execute($"PRAGMA user_version = {migration.Version}");
            });
        }
    }

    public async Task RunInTransactionAsync(Func<Task> operation)
    {
        await InitializeAsync();

        if (_inTransaction.Value)
        {
            await operation();
            return;
        }

        await _writeGate.WaitAsync();
        _inTransaction.Value = true;
        try
        {
            await _connection.ExecuteAsync("BEGIN TRANSACTION");
            try
            {
                await operation();
                await _connection.ExecuteAsync("COMMIT");
            }
            catch
            {
                try
                {
                    await _connection.ExecuteAsync("ROLLBACK");
                }
                catch (SQLiteException)
                {
                    // SQLite may have rolled back on its own (disk full); keep the original failure.
                }

                throw;
            }
        }
        finally
        {
            _inTransaction.Value = false;
            _writeGate.Release();
        }
    }

    public async Task<IReadOnlyList<ExampleEntry>> GetEntriesAsync()
    {
        await InitializeAsync();

        List<ExampleEntryEntity> rows = await _connection.QueryAsync<ExampleEntryEntity>(
            "SELECT * FROM ExampleEntries ORDER BY Date, Id");
        return [.. rows.Select(ToModel)];
    }

    public Task<int> SaveEntryAsync(ExampleEntry entry) => WriteAsync(async () =>
    {
        ExampleEntryEntity row = ToEntity(entry);
        if (row.Id == 0)
        {
            await _connection.InsertAsync(row);
        }
        else
        {
            await _connection.UpdateAsync(row);
        }

        return row.Id;
    });

    public Task DeleteEntryAsync(int id) =>
        WriteAsync(() => _connection.ExecuteAsync("DELETE FROM ExampleEntries WHERE Id = ?", id));

    public Task DeleteAllEntriesAsync() =>
        WriteAsync(() => _connection.ExecuteAsync("DELETE FROM ExampleEntries"));

    /// <summary>Releases the file handle (shared by every connection to the same path).</summary>
    public async ValueTask DisposeAsync() => await _connection.CloseAsync();

    /// <summary>
    /// Waits for any in-flight transaction so this write isn't swept into it. A write made by
    /// the transaction body itself passes straight through.
    /// </summary>
    private async Task<T> WriteAsync<T>(Func<Task<T>> write)
    {
        await InitializeAsync();

        bool gated = !_inTransaction.Value;
        if (gated)
        {
            await _writeGate.WaitAsync();
        }

        try
        {
            return await write();
        }
        finally
        {
            if (gated)
            {
                _writeGate.Release();
            }
        }
    }

    private static ExampleEntry ToModel(ExampleEntryEntity row) => new()
    {
        Id = row.Id,
        Date = IsoDate.Parse(row.Date),
        Title = row.Title,
        MassKilograms = row.MassKilograms,
        CreatedAt = IsoDate.ParseTimestamp(row.CreatedAt),
    };

    private static ExampleEntryEntity ToEntity(ExampleEntry entry) => new()
    {
        Id = entry.Id,
        Date = IsoDate.ToStorage(entry.Date),
        Title = entry.Title,
        MassKilograms = entry.MassKilograms,
        CreatedAt = IsoDate.TimestampToStorage(entry.CreatedAt),
    };
}
