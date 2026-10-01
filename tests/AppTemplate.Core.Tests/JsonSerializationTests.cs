using System.Text.Json;
using AppTemplate.Core.Models;
using FluentAssertions;

namespace AppTemplate.Core.Tests;

[TestClass]
public class JsonSerializationTests
{
    [TestMethod]
    public void ReflectionSerialization_IsDisabledInTests()
    {
        JsonSerializer.IsReflectionEnabledByDefault.Should().BeFalse();
    }

    [TestMethod]
    public void Serialize_ReflectionOverload_Throws()
    {
        Action act = () => JsonSerializer.Serialize(new ExampleModel());

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void RoundTrip_ThroughSourceGeneratedContext_Works()
    {
        ExampleModel model = new();

        string json = JsonSerializer.Serialize(model, AppTemplateJsonContext.Default.ExampleModel);
        ExampleModel? result = JsonSerializer.Deserialize(json, AppTemplateJsonContext.Default.ExampleModel);

        result.Should().NotBeNull();
    }
}
