using JetBrains.Annotations;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.PricingEngines.swaption
{
    [PublicAPI]
    public class BachelierSpec : ISwaptionEngineSpec
    {
        private VolatilityType type_;

        public BachelierSpec()
        {
            type_ = VolatilityType.Normal;
        }

        public VolatilityType type() => type_;

        public double value(QLNet.Option.Type type, double strike, double atmForward, double stdDev, double annuity,
            double displacement = 0.0) =>
            Utils.bachelierBlackFormula(type, strike, atmForward, stdDev, annuity);

        public double vega(double strike, double atmForward, double stdDev, double exerciseTime, double annuity,
            double displacement = 0.0) =>
            System.Math.Sqrt(exerciseTime) *
            Utils.bachelierBlackFormulaStdDevDerivative(strike, atmForward, stdDev, annuity);
    }
}
