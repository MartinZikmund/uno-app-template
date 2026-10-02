namespace AppTemplate.Core.Services.Data;

public interface IDataService
{
    /// <summary>Opens the database and applies pending migrations. Other members call it lazily.</summary>
    Task InitializeAsync();

    /// <summary>
    /// Runs <paramref name="operation"/> as one transaction: everything it writes through this
    /// service commits together or rolls back together. Writes from elsewhere wait until it ends.
    /// </summary>
    Task RunInTransactionAsync(Func<Task> operation);

    Task<IReadOnlyList<ExampleEntry>> GetEntriesAsync();

    /// <summary>Inserts when <see cref="ExampleEntry.Id"/> is 0, otherwise updates. Returns the id.</summary>
    Task<int> SaveEntryAsync(ExampleEntry entry);

    Task DeleteEntryAsync(int id);

    Task DeleteAllEntriesAsync();
}
