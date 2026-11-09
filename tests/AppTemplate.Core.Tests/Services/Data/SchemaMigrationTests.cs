using AppTemplate.Core.Services.Data;
using FluentAssertions;

namespace AppTemplate.Core.Tests.Services.Data;

[TestClass]
public class SchemaMigrationTests
{
    private TempDatabase _db = null!;

    [TestInitialize]
    public void Initialize() => _db = new();

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task InitializeAsync_FreshDatabase_AppliesEveryMigration()
    {
        await using SqliteDataService service = new(_db.Path);

        await service.InitializeAsync();

        _db.ReadUserVersion().Should().Be(SchemaMigrations.LatestVersion);
        _db.TableExists("ExampleEntries").Should().BeTrue();
    }

    [TestMethod]
    public async Task InitializeAsync_CalledAgain_DoesNotReapplyMigrations()
    {
        int applied = 0;
        SchemaMigration[] migrations = [new(1, _ => applied++)];

        await using (SqliteDataService first = new(_db.Path, migrations))
        {
            await first.InitializeAsync();
            await first.InitializeAsync();
        }

        await using SqliteDataService second = new(_db.Path, migrations);
        await second.InitializeAsync();

        applied.Should().Be(1);
    }

    [TestMethod]
    public async Task InitializeAsync_OlderDatabase_UpgradesAndKeepsData()
    {
        await using (SqliteDataService v1 = new(_db.Path, [.. SchemaMigrations.All.Where(m => m.Version == 1)]))
        {
            await v1.SaveEntryAsync(new ExampleEntry { Date = new(2026, 4, 1), Title = "Kept", MassKilograms = 2 });
        }

        SchemaMigration[] upgraded =
        [
            .. SchemaMigrations.All.Where(m => m.Version == 1),
            new(2, c => c.Execute("ALTER TABLE ExampleEntries ADD COLUMN Notes TEXT")),
        ];
        await using SqliteDataService v2 = new(_db.Path, upgraded);
        await v2.InitializeAsync();

        _db.ReadUserVersion().Should().Be(2);
        _db.ColumnExists("ExampleEntries", "Notes").Should().BeTrue();
        (await v2.GetEntriesAsync()).Should().ContainSingle(e => e.Title == "Kept");
    }

    [TestMethod]
    public async Task InitializeAsync_MigrationThrows_RollsBackItsWorkAndVersion()
    {
        SchemaMigration[] migrations =
        [
            .. SchemaMigrations.All.Where(m => m.Version == 1),
            new(2, c =>
            {
                c.Execute("CREATE TABLE HalfDone (Id INTEGER)");
                throw new InvalidOperationException("process killed mid-migration");
            }),
        ];
        await using SqliteDataService service = new(_db.Path, migrations);

        Func<Task> act = service.InitializeAsync;

        await act.Should().ThrowAsync<InvalidOperationException>();
        _db.ReadUserVersion().Should().Be(1, "a migration that failed must not leave a version claiming it ran");
        _db.TableExists("HalfDone").Should().BeFalse("the migration's work rolls back with its version bump");
    }

    [TestMethod]
    public async Task InitializeAsync_AfterFailedMigration_CanRetry()
    {
        bool fail = true;
        SchemaMigration[] migrations =
        [
            new(1, c =>
            {
                if (fail)
                {
                    throw new InvalidOperationException();
                }
            }),
        ];
        await using SqliteDataService service = new(_db.Path, migrations);
        await service.Invoking(s => s.InitializeAsync()).Should().ThrowAsync<InvalidOperationException>();

        fail = false;
        await service.InitializeAsync();

        _db.ReadUserVersion().Should().Be(1);
    }

    [TestMethod]
    public async Task InitializeAsync_DatabaseFromNewerApp_Throws()
    {
        await using (SqliteDataService current = new(_db.Path))
        {
            await current.InitializeAsync();
        }

        _db.SetUserVersion(SchemaMigrations.LatestVersion + 1);
        await using SqliteDataService service = new(_db.Path);

        await service.Invoking(s => s.InitializeAsync()).Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public void All_VersionsAreConsecutiveFromOne() =>
        SchemaMigrations.All.Select(m => m.Version).Should().Equal(Enumerable.Range(1, SchemaMigrations.All.Count));
}
