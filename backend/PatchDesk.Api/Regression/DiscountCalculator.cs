namespace PatchDesk.Api.Regression;

public static class DiscountCalculator
{
    public static decimal Calculate(decimal subtotal, decimal percent)
    {
        if (subtotal < 0) throw new ArgumentOutOfRangeException(nameof(subtotal));
        if (percent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percent));
        return subtotal >= 100m ? decimal.Round(subtotal * (1m - percent / 100m), 2, MidpointRounding.AwayFromZero) : subtotal;
    }
}
// An isolated historical defect fixture, used only by the explicit before/after demonstration.
public static class DefectiveDiscountFixture
{
    public static decimal Calculate(decimal subtotal, decimal percent) =>
        subtotal > 100m ? decimal.Round(subtotal * (1m - percent / 100m), 2, MidpointRounding.AwayFromZero) : subtotal;
}
