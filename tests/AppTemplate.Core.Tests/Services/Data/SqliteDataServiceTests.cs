using System.Globalization;
using AppTemplate.Core.Services.Data;
using FluentAssertions;

namespace AppTemplate.Core.Tests.Services.Data;

[TestClass]
public class SqliteDataServiceTests
{
    private TempDatabase _db = null!;
    private SqliteDataService _service = null!;

    [TestInitialize]
    public void Initialize()
    {
        _db = new();
        _service = new(_db.Path);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _service.DisposeAsync();
        _db.Dispose();
    }

    [TestMethod]
    public async Task SaveEntryAsync_NewEntry_RoundTripsEveryField()
    {
        ExampleEntry entry = new()
        {
            Date = new(2026, 2, 28),
            Title = "Sample",
            MassKilograms = 12.5,
            CreatedAt = new(2026, 2, 28, 8, 30, 0, DateTimeKind.Utc),
        };

        int id = await _service.SaveEntryAsync(entry);

        (await _service.GetEntriesAsync()).Should().ContainSingle().Which.Should().Be(entry with { Id = id });
    }

    [TestMethod]
    public async Task SaveEntryAsync_ExistingId_Updates()
    {
        int id = await _service.SaveEntryAsync(new() { Date = new(2026, 1, 1), Title = "Before" });

        await _service.SaveEntryAsync(new() { Id = id, Date = new(2026, 1, 1), Title = "After" });

        (await _service.GetEntriesAsync()).Should().ContainSingle().Which.Title.Should().Be("After");
    }

    [TestMethod]
    public async Task DeleteEntryAsync_RemovesOnlyThatEntry()
    {
        int keep = await _service.SaveEntryAsync(new() { Date = new(2026, 1, 1), Title = "Keep" });
        int remove = await _service.SaveEntryAsync(new() { Date = new(2026, 1, 2), Title = "Remove" });

        await _service.DeleteEntryAsync(remove);

        (await _service.GetEntriesAsync()).Should().ContainSingle().Which.Id.Should().Be(keep);
    }

    [TestMethod]
    public async Task SaveEntryAsync_UnderNonGregorianCulture_StoresIsoDate()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fa-IR");
            await _service.SaveEntryAsync(new() { Date = new(2026, 7, 4), Title = "Persian" });
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        _db.Query(c => c.ExecuteScalar<string>("SELECT Date FROM ExampleEntries")).Should().Be("2026-07-04");
    }

    [TestMethod]
    public async Task Constructor_MissingFolder_IsCreated()
    {
        string folder = Path.Combine(Path.GetTempPath(), "AppTemplate.Tests", Guid.NewGuid().ToString("N"));
        await using SqliteDataService service = new(Path.Combine(folder, "nested.db"));

        await service.InitializeAsync();

        Directory.Exists(folder).Should().BeTrue();
    }
}
