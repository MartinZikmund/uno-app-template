namespace AppTemplate.Services.Logging;

public static class ErrorMessage
{
    /// <summary>
    /// Joins the parts as separate paragraphs. The exception text is appended rather than
    /// formatted into a sentence, so translations don't have to bend grammar around it.
    /// </summary>
    public static string Compose(string explanation, Exception? exception, string? logHint)
    {
        string?[] parts = [explanation, exception?.Message, logHint];
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
