# SQLite storage

You want to keep the user's data in a local database, change its shape over time without
breaking existing installs, and let people back it up and restore it. The template ships a small
scaffold for exactly that, built on [sqlite-net](https://github.com/praeclarum/sqlite-net)
(`sqlite-net-e` + `SourceGear.sqlite3`).

Everything lives in `src/AppTemplate.Core/Services/Data/`, so it is covered by unit tests that run
against a real database file — no UI head needed.

| Type | What it is |
|---|---|
| `IDataService` / `SqliteDataService` | Data access, migrations, transactions. |
| `SchemaMigrations` | The schema history, keyed on `PRAGMA user_version`. |
| `IsoDate` | The one place dates become text. |
| `MassUnits` | Example of converting a canonical unit at the UI edge. |
| `IBackupService` / `BackupService` | JSON export and all-or-nothing restore. |
| `ExampleEntry` | **Placeholder.** One sample table so the rest has something to work on. |

`ExampleEntry`, its `ExampleEntries` table, and the `*EntriesAsync` methods are there to be
replaced. Rename or delete them once you have your own model.

## Wiring

Both services are singletons registered in `App.RegisterServices`. The database file is
`app.db` in `ApplicationData.Current.LocalFolder`:

```csharp
services.AddSingleton<IDataService>(_ => new SqliteDataService(
    Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "app.db")));
services.AddSingleton<IBackupService, BackupService>();
```

Nothing opens the database at startup. The first call into `IDataService` runs
`InitializeAsync()` (and with it any pending migrations). If you'd rather surface a migration
failure before the first page shows, call `InitializeAsync()` yourself during launch.

Tests construct the service directly with a temp path — see `TempDatabase` in
`tests/AppTemplate.Core.Tests/Services/Data/`:

```csharp
await using SqliteDataService service = new(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db"));
```

## Changing the schema

Append a step to `SchemaMigrations.All`, with the next version number:

```csharp
new(2, c => c.Execute("ALTER TABLE ExampleEntries ADD COLUMN Notes TEXT")),
```

Rules:

- **Never edit a migration that has shipped.** Existing databases already ran it.
- **Write the schema in SQL**, not `CreateTable<T>()`. `CreateTable` silently adds missing
  columns, so the database shape would depend on the code that happened to open it, not on its
  version.
- Each step **and its version bump commit in one transaction** (`user_version` lives in the
  database header and rolls back with everything else). If the process is killed part way
  through, the next launch sees the old version and the old schema, and simply tries again —
  instead of re-running a half-recorded data migration on top of itself.
- A database with a higher version than the app knows (the user downgraded) makes
  `InitializeAsync` throw rather than guess.

## Transactions

```csharp
await dataService.RunInTransactionAsync(async () =>
{
    await dataService.DeleteAllEntriesAsync();
    await dataService.SaveEntryAsync(entry);
});
```

Everything the body writes through the service commits together or not at all. A nested
`RunInTransactionAsync` joins the outer one.

sqlite-net locks the connection per statement, not per transaction. Without help, a write from
somewhere else (say the UI saving while an import runs) would slip in between `BEGIN` and
`COMMIT` and get thrown away by a rollback it had nothing to do with. `SqliteDataService` makes
writes from outside the transaction wait until it ends. Every new write method should go through
the private `WriteAsync` helper to get that behavior.

Reads are not gated, so a read from elsewhere during a transaction can see its uncommitted rows.

## Dates and units

- **Dates:** always go through `IsoDate` (`yyyy-MM-dd`, invariant culture; timestamps are
  round-trip `"O"` format). A date formatted with the current culture can't be read back after
  the user switches to a language with a different calendar or format — Persian, for one.
- **Units:** store one canonical unit (here kilograms, in a column whose name says so) and
  convert only where the value meets the user, with something like `MassUnits.ToDisplay` /
  `FromDisplay`. Changing a unit preference then never needs to touch stored data.

## Backup and restore

`IBackupService.ExportAsync()` returns a full snapshot as indented JSON: every entry plus the
settings worth carrying over (currently the theme). `ImportAsync(json)` replaces the current data
with it:

1. The file is parsed; a missing `version` means the file predates versioning and is read as
   `BackupFormat.LegacyVersion`. Versions newer than `BackupFormat.CurrentVersion` are refused.
2. **Everything is validated before anything is written.** One bad row rejects the whole file.
3. Settings are applied **before** data, so anything that reads them while the data lands sees
   the backup's.
4. Data is replaced inside one transaction. If writing fails, the data rolls back and the
   previous settings are put back by hand (they aren't in the database).

The result is a `BackupImportResult` with a `BackupImportError` code instead of a message, so the
UI can show a localized string.

When the format changes, bump `BackupFormat.CurrentVersion` and convert older files in
`BackupService.ImportAsync` after the version check.

Serialization goes through the source-generated `BackupJsonContext` — see
[json-aot-serialization.md](./json-aot-serialization.md) for why.
