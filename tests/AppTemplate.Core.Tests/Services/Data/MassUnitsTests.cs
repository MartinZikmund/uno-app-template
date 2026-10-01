using AppTemplate.Core.Services.Data;
using FluentAssertions;

namespace AppTemplate.Core.Tests.Services.Data;

[TestClass]
public class MassUnitsTests
{
    [TestMethod]
    public void ToDisplay_Metric_ReturnsKilograms() =>
        MassUnits.ToDisplay(80, useMetric: true).Should().Be(80);

    [TestMethod]
    public void ToDisplay_Imperial_ReturnsPounds() =>
        MassUnits.ToDisplay(80, useMetric: false).Should().BeApproximately(176.37, 0.01);

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void FromDisplay_RoundTripsToDisplay(bool useMetric) =>
        MassUnits.FromDisplay(MassUnits.ToDisplay(72.5, useMetric), useMetric).Should().BeApproximately(72.5, 1e-9);
}
