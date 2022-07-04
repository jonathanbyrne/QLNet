using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Pricingengines.swaption
{
    [JetBrains.Annotations.PublicAPI] public class Black76Spec : ISwaptionEngineSpec
    {
        private VolatilityType type_;

        public VolatilityType type() => type_;

        public Black76Spec()
        {
            type_ = VolatilityType.ShiftedLognormal;
        }

        public double value(QLNet.Option.Type type, double strike, double atmForward, double stdDev, double annuity,
            double displacement = 0.0) =>
            Utils.blackFormula(type, strike, atmForward, stdDev, annuity, displacement);

        public double vega(double strike, double atmForward, double stdDev, double exerciseTime, double annuity,
            double displacement = 0.0) =>
            System.Math.Sqrt(exerciseTime) *
            Utils.blackFormulaStdDevDerivative(strike, atmForward, stdDev, annuity, displacement);
    }
}