using System.Text.Json.Nodes;
using AppTemplate.Core.Services.Data;
using AppTemplate.Core.Tests.Fakes;
using FluentAssertions;
using Microsoft.UI.Xaml;

namespace AppTemplate.Core.Tests.Services.Data;

[TestClass]
public class BackupServiceTests
{
    private TempDatabase _db = null!;
    private SqliteDataService _data = null!;
    private FakeAppPreferences _preferences = null!;

    [TestInitialize]
    public void Initialize()
    {
        _db = new();
        _data = new(_db.Path);
        _preferences = new() { Theme = ElementTheme.Light };
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _data.DisposeAsync();
        _db.Dispose();
    }

    private BackupService CreateService(IDataService? data = null) => new(data ?? _data, _preferences);

    private static ExampleEntry Entry(string title, int day = 1) => new()
    {
        Date = new(2026, 3, day),
        Title = title,
        MassKilograms = day,
        CreatedAt = new(2026, 3, day, 10, 0, 0, DateTimeKind.Utc),
    };

    [TestMethod]
    public async Task ExportAsync_ThenImportIntoEmptyDatabase_RestoresEverything()
    {
        await _data.SaveEntryAsync(Entry("a", 1));
        await _data.SaveEntryAsync(Entry("b", 2));
        string json = await CreateService().ExportAsync();

        await _data.DeleteAllEntriesAsync();
        _preferences.Theme = ElementTheme.Dark;
        BackupImportResult result = await CreateService().ImportAsync(json);

        result.Should().Be(BackupImportResult.Succeeded(2));
        (await _data.GetEntriesAsync()).Select(e => e with { Id = 0 })
            .Should().BeEquivalentTo([Entry("a", 1), Entry("b", 2)]);
        _preferences.Theme.Should().Be(ElementTheme.Light);
    }

    [TestMethod]
    public async Task ExportAsync_WritesCurrentVersionAndIsoDates()
    {
        await _data.SaveEntryAsync(Entry("a", 9));

        JsonNode json = JsonNode.Parse(await CreateService().ExportAsync())!;

        json["version"]!.GetValue<int>().Should().Be(BackupFormat.CurrentVersion);
        json["entries"]![0]!["date"]!.GetValue<string>().Should().Be("2026-03-09");
    }

    [TestMethod]
    public async Task ImportAsync_ReplacesExistingData()
    {
        await _data.SaveEntryAsync(Entry("old"));
        string json = """{ "version": 1, "entries": [ { "date": "2026-01-01", "title": "new", "massKilograms": 1, "createdAt": "2026-01-01T00:00:00Z" } ] }""";

        await CreateService().ImportAsync(json);

        (await _data.GetEntriesAsync()).Should().ContainSingle().Which.Title.Should().Be("new");
    }

    [TestMethod]
    public async Task ImportAsync_NoVersion_IsReadAsLegacy()
    {
        string json = """{ "entries": [ { "date": "2026-01-01", "title": "legacy", "massKilograms": 1, "createdAt": "2026-01-01T00:00:00Z" } ] }""";

        BackupImportResult result = await CreateService().ImportAsync(json);

        result.Should().Be(BackupImportResult.Succeeded(1));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(BackupFormat.CurrentVersion + 1)]
    public async Task ImportAsync_UnsupportedVersion_FailsWithoutWriting(int version)
    {
        await _data.SaveEntryAsync(Entry("existing"));

        BackupImportResult result = await CreateService().ImportAsync($$"""{ "version": {{version}}, "entries": [] }""");

        result.Error.Should().Be(BackupImportError.UnsupportedVersion);
        (await _data.GetEntriesAsync()).Should().ContainSingle();
    }

    [TestMethod]
    [DataRow("not json")]
    [DataRow("null")]
    [DataRow("[]")]
    public async Task ImportAsync_Malformed_FailsAsInvalidFormat(string json) =>
        (await CreateService().ImportAsync(json)).Error.Should().Be(BackupImportError.InvalidFormat);

    [TestMethod]
    [DataRow("""{ "date": "31/01/2026", "title": "t", "massKilograms": 1, "createdAt": "2026-01-01T00:00:00Z" }""")]
    [DataRow("""{ "date": "2026-01-01", "title": "", "massKilograms": 1, "createdAt": "2026-01-01T00:00:00Z" }""")]
    [DataRow("""{ "date": "2026-01-01", "title": "t", "massKilograms": -1, "createdAt": "2026-01-01T00:00:00Z" }""")]
    [DataRow("""{ "date": "2026-01-01", "title": "t", "massKilograms": 1, "createdAt": "yesterday" }""")]
    public async Task ImportAsync_AnyInvalidRow_RejectsWholeFileBeforeWriting(string badEntry)
    {
        await _data.SaveEntryAsync(Entry("existing"));
        _preferences.Theme = ElementTheme.Dark;
        string json = $$"""
            {
              "version": 1,
              "settings": { "theme": "Light" },
              "entries": [
                { "date": "2026-01-01", "title": "good", "massKilograms": 1, "createdAt": "2026-01-01T00:00:00Z" },
                {{badEntry}}
              ]
            }
            """;

        BackupImportResult result = await CreateService().ImportAsync(json);

        result.Error.Should().Be(BackupImportError.InvalidData);
        (await _data.GetEntriesAsync()).Should().ContainSingle().Which.Title.Should().Be("existing");
        _preferences.Theme.Should().Be(ElementTheme.Dark, "nothing is applied until everything validates");
    }

    [TestMethod]
    public async Task ImportAsync_UnknownTheme_RejectsWholeFile()
    {
        string json = """{ "version": 1, "settings": { "theme": "Purple" }, "entries": [] }""";

        (await CreateService().ImportAsync(json)).Error.Should().Be(BackupImportError.InvalidData);
    }

    [TestMethod]
    public async Task ImportAsync_AppliesSettingsBeforeData()
    {
        _preferences.Theme = ElementTheme.Dark;
        RecordingDataService recording = new(_data, onSave: () => _preferences.Theme);
        string json = """{ "version": 1, "settings": { "theme": "Light" }, "entries": [ { "date": "2026-01-01", "title": "t", "massKilograms": 1, "createdAt": "2026-01-01T00:00:00Z" } ] }""";

        await CreateService(recording).ImportAsync(json);

        recording.ThemeAtFirstSave.Should().Be(ElementTheme.Light);
    }

    [TestMethod]
    public async Task ImportAsync_NoSettings_LeavesCurrentSettingsAlone()
    {
        _preferences.Theme = ElementTheme.Dark;

        await CreateService().ImportAsync("""{ "version": 1, "entries": [] }""");

        _preferences.Theme.Should().Be(ElementTheme.Dark);
    }

    [TestMethod]
    public async Task ImportAsync_WriteFailsPartWay_RollsBackDataAndSettings()
    {
        await _data.SaveEntryAsync(Entry("existing"));
        _preferences.Theme = ElementTheme.Dark;
        RecordingDataService failing = new(_data, failOnSave: 2);
        string json = """
            {
              "version": 1,
              "settings": { "theme": "Light" },
              "entries": [
                { "date": "2026-01-01", "title": "first", "massKilograms": 1, "createdAt": "2026-01-01T00:00:00Z" },
                { "date": "2026-01-02", "title": "second", "massKilograms": 1, "createdAt": "2026-01-02T00:00:00Z" }
              ]
            }
            """;

        BackupImportResult result = await CreateService(failing).ImportAsync(json);

        result.Error.Should().Be(BackupImportError.WriteFailed);
        (await _data.GetEntriesAsync()).Should().ContainSingle().Which.Title.Should().Be("existing");
        _preferences.Theme.Should().Be(ElementTheme.Dark);
    }

    /// <summary>Passes through to a real service, but can observe or sabotage saves.</summary>
    private sealed class RecordingDataService(
        IDataService inner,
        Func<ElementTheme>? onSave = null,
        int failOnSave = 0) : IDataService
    {
        private int _saves;

        public ElementTheme? ThemeAtFirstSave { get; private set; }

        public Task InitializeAsync() => inner.InitializeAsync();

        public Task RunInTransactionAsync(Func<Task> operation) => inner.RunInTransactionAsync(operation);

        public Task<IReadOnlyList<ExampleEntry>> GetEntriesAsync() => inner.GetEntriesAsync();

        public Task<int> SaveEntryAsync(ExampleEntry entry)
        {
            _saves++;
            ThemeAtFirstSave ??= onSave?.Invoke();
            if (_saves == failOnSave)
            {
                throw new IOException("disk full");
            }

            return inner.SaveEntryAsync(entry);
        }

        public Task DeleteEntryAsync(int id) => inner.DeleteEntryAsync(id);

        public Task DeleteAllEntriesAsync() => inner.DeleteAllEntriesAsync();
    }
}
