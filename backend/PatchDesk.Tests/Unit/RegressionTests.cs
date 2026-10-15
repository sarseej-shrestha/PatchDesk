using PatchDesk.Api.Regression;
namespace PatchDesk.Tests.Unit;
public class RegressionTests
{
    [Theory]
    [InlineData("discount.boundary")]
    [InlineData("discount.full")]
    [InlineData("discount.rounding")]
    public void CorrectedImplementationPassesTheRegisteredAssertion(string identifier)
    {
        for (var i = 0; i < 3; i++) Assert.True(RegressionRegistry.Run(identifier, false).Passed);
    }
    [Fact]
    public void SameBoundaryAssertionCatchesDefectAndAcceptsCorrection()
    {
        var before = RegressionRegistry.Run("discount.boundary", true);
        var after = RegressionRegistry.Run("discount.boundary", false);
        Assert.False(before.Passed); Assert.Contains("actual=100", before.Evidence);
        Assert.True(after.Passed); Assert.Contains("actual=90", after.Evidence);
    }
    [Fact]
    public void UnknownTestsCannotExecute() => Assert.Throws<ArgumentException>(() => RegressionRegistry.Run("shell:rm", false));
    [Theory]
    [InlineData(-1, 10)]
    [InlineData(100, -1)]
    [InlineData(100, 101)]
    public void CalculatorRejectsInvalidInput(decimal subtotal, decimal percent) => Assert.Throws<ArgumentOutOfRangeException>(() => DiscountCalculator.Calculate(subtotal, percent));
}
