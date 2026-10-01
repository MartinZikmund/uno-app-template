using AppTemplate.Services.Logging;
using FluentAssertions;

namespace AppTemplate.Core.Tests.Services.Logging;

[TestClass]
public class ErrorMessageTests
{
    private static readonly string Gap = Environment.NewLine + Environment.NewLine;

    [TestMethod]
    public void Compose_WithExplanationOnly_ReturnsExplanation()
    {
        var message = ErrorMessage.Compose("Something broke.", exception: null, logHint: null);

        message.Should().Be("Something broke.");
    }

    [TestMethod]
    public void Compose_WithException_AppendsExceptionMessageAfterExplanation()
    {
        var message = ErrorMessage.Compose("Something broke.", new InvalidOperationException("Disk full"), logHint: null);

        message.Should().Be("Something broke." + Gap + "Disk full");
    }

    [TestMethod]
    public void Compose_WithLogHint_AppendsHintLast()
    {
        var message = ErrorMessage.Compose(
            "Something broke.",
            new InvalidOperationException("Disk full"),
            @"Logs are in C:\Logs");

        message.Should().Be("Something broke." + Gap + "Disk full" + Gap + @"Logs are in C:\Logs");
    }

    [TestMethod]
    public void Compose_WithBlankParts_SkipsThem()
    {
        var message = ErrorMessage.Compose("Something broke.", new Exception(" "), logHint: "  ");

        message.Should().Be("Something broke.");
    }
}
