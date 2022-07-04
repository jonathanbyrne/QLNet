using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Pricingengines.swaption
{
    [JetBrains.Annotations.PublicAPI] public interface ISwaptionEngineSpec
    {
        VolatilityType type();

        double value(QLNet.Option.Type type, double strike, double atmForward, double stdDev, double annuity,
            double displacement = 0.0);

        double vega(double strike, double atmForward, double stdDev, double exerciseTime, double annuity,
            double displacement = 0.0);
    }
}