using AppTemplate.Services.Dialogs;

namespace AppTemplate.Core.Tests.Fakes;

internal sealed class FakeErrorDialogService : IErrorDialogService
{
    public List<(string Title, string Message, Exception? Exception)> Shown { get; } = [];

    public Task ShowAsync(string title, string message, Exception? exception = null)
    {
        Shown.Add((title, message, exception));
        return Task.CompletedTask;
    }
}
