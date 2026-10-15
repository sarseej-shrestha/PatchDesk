using System.Diagnostics;
namespace PatchDesk.Api.Regression;
public record RegisteredTest(string Identifier, string Name, string Description, decimal Subtotal, decimal Percent, decimal Expected);
public record RegistryResult(bool Passed, string Evidence, long DurationMilliseconds);
public static class RegressionRegistry
{
    public static IReadOnlyList<RegisteredTest> Tests { get; } = [
        new("discount.boundary", "Discount at the $100 boundary", "A 10% discount on $100 must produce $90.", 100m, 10m, 90m),
        new("discount.full", "Full discount at the boundary", "A 100% discount on $100 must produce $0.", 100m, 100m, 0m),
        new("discount.rounding", "Round invoice totals to cents", "A 10% discount on $100.05 rounds to $90.05, away from zero.", 100.05m, 10m, 90.05m)
    ];
    public static RegistryResult Run(string identifier, bool defective)
    {
        var test = Tests.SingleOrDefault(t => t.Identifier == identifier) ?? throw new ArgumentException("Unknown registry identifier.", nameof(identifier));
        var timer = Stopwatch.StartNew();
        var actual = defective ? DefectiveDiscountFixture.Calculate(test.Subtotal, test.Percent) : DiscountCalculator.Calculate(test.Subtotal, test.Percent);
        timer.Stop();
        return new(actual == test.Expected, FormattableString.Invariant($"{identifier}: subtotal={test.Subtotal}; discount={test.Percent}%; expected={test.Expected}; actual={actual}; fixture={(defective ? "defective" : "corrected")}"), timer.ElapsedMilliseconds);
    }
}
