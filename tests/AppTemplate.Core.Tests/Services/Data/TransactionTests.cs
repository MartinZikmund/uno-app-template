using AppTemplate.Core.Services.Data;
using FluentAssertions;

namespace AppTemplate.Core.Tests.Services.Data;

[TestClass]
public class TransactionTests
{
    private TempDatabase _db = null!;
    private SqliteDataService _service = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = new();
        _service = new(_db.Path);
        await _service.InitializeAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _service.DisposeAsync();
        _db.Dispose();
    }

    private static ExampleEntry Entry(string title) =>
        new() { Date = new(2026, 1, 1), Title = title, MassKilograms = 1 };

    [TestMethod]
    public async Task RunInTransactionAsync_BodySucceeds_CommitsEveryWrite()
    {
        await _service.RunInTransactionAsync(async () =>
        {
            await _service.SaveEntryAsync(Entry("a"));
            await _service.SaveEntryAsync(Entry("b"));
        });

        (await _service.GetEntriesAsync()).Select(e => e.Title).Should().BeEquivalentTo(["a", "b"]);
    }

    [TestMethod]
    public async Task RunInTransactionAsync_BodyThrows_RollsBackEveryWrite()
    {
        await _service.SaveEntryAsync(Entry("existing"));

        Func<Task> act = () => _service.RunInTransactionAsync(async () =>
        {
            await _service.DeleteAllEntriesAsync();
            await _service.SaveEntryAsync(Entry("half-written"));
            throw new InvalidOperationException("boom");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        (await _service.GetEntriesAsync()).Should().ContainSingle(e => e.Title == "existing");
    }

    [TestMethod]
    public async Task RunInTransactionAsync_UnrelatedWriteDuringTransaction_SurvivesRollback()
    {
        TaskCompletionSource bodyStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseBody = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task transaction = _service.RunInTransactionAsync(async () =>
        {
            await _service.SaveEntryAsync(Entry("inside"));
            bodyStarted.SetResult();
            await releaseBody.Task;
            throw new InvalidOperationException("rolled back");
        });

        await bodyStarted.Task;
        // A write from another flow, e.g. the UI saving while an import is running.
        Task unrelated = Task.Run(() => _service.SaveEntryAsync(Entry("unrelated")));
        await Task.Delay(100);
        unrelated.IsCompleted.Should().BeFalse("it must wait for the transaction instead of joining it");

        releaseBody.SetResult();
        await transaction.Invoking(t => t).Should().ThrowAsync<InvalidOperationException>();
        await unrelated;

        (await _service.GetEntriesAsync()).Should().ContainSingle(e => e.Title == "unrelated");
    }

    [TestMethod]
    public async Task RunInTransactionAsync_Nested_JoinsTheOuterTransaction()
    {
        Func<Task> act = () => _service.RunInTransactionAsync(async () =>
        {
            await _service.RunInTransactionAsync(() => _service.SaveEntryAsync(Entry("nested")));
            throw new InvalidOperationException();
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await _service.GetEntriesAsync()).Should().BeEmpty("the inner work belongs to the outer transaction");
    }
}
