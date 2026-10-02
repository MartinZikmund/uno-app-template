using System.Text.Json;
using AppTemplate.Core.Models;
using FluentAssertions;

namespace AppTemplate.Core.Tests;

[TestClass]
public class JsonSerializationTests
{
    [TestMethod]
    public void IsReflectionEnabledByDefault_TestHost_IsFalse()
    {
        JsonSerializer.IsReflectionEnabledByDefault.Should().BeFalse();
    }

    [TestMethod]
    public void Serialize_ReflectionOverload_ThrowsInvalidOperationException()
    {
        Action act = () => JsonSerializer.Serialize(new ExampleModel());

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void RoundTrip_SourceGeneratedContext_PreservesMembers()
    {
        ExampleModel model = new() { Id = 42, Name = "Example" };

        string json = JsonSerializer.Serialize(model, AppTemplateJsonContext.Default.ExampleModel);
        ExampleModel? result = JsonSerializer.Deserialize(json, AppTemplateJsonContext.Default.ExampleModel);

        result.Should().Be(model);
    }
}
