using QLNet.Termstructures.Volatility;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Termstructures.Volatility.swaption;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class BlackVanillaOptionPricer : VanillaOptionPricer
    {
        private double forwardValue_;
        private Date expiryDate_;
        private Period swapTenor_;
        private SwaptionVolatilityStructure volatilityStructure_;
        private SmileSection smile_;

        public BlackVanillaOptionPricer(double forwardValue, Date expiryDate, Period swapTenor, SwaptionVolatilityStructure volatilityStructure)
        {
            forwardValue_ = forwardValue;
            expiryDate_ = expiryDate;
            swapTenor_ = swapTenor;
            volatilityStructure_ = volatilityStructure;
            smile_ = volatilityStructure_.smileSection(expiryDate_, swapTenor_);

            Utils.QL_REQUIRE(volatilityStructure.volatilityType() == VolatilityType.ShiftedLognormal &&
                             Utils.close_enough(volatilityStructure.shift(expiryDate, swapTenor), 0.0), () =>
                "BlackVanillaOptionPricer: zero-shift lognormal volatility required");
        }

        public override double value(double strike, QLNet.Option.Type optionType, double deflator)
        {
            var variance = smile_.variance(strike);
            return deflator * Utils.blackFormula(optionType, strike, forwardValue_, System.Math.Sqrt(variance));
        }
    }
}