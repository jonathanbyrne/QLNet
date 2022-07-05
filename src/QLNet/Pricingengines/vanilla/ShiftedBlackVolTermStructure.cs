using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Time;

namespace QLNet.Pricingengines.vanilla
{
    [PublicAPI]
    public class ShiftedBlackVolTermStructure : BlackVolTermStructure
    {
        private double varianceOffset_;
        private Handle<BlackVolTermStructure> volTS_;

        public ShiftedBlackVolTermStructure(double varianceOffset, Handle<BlackVolTermStructure> volTS)
            : base(volTS.link.referenceDate(), volTS.link.calendar(), BusinessDayConvention.Following, volTS.link.dayCounter())
        {
            varianceOffset_ = varianceOffset;
            volTS_ = volTS;
        }

        public override Date maxDate() => volTS_.link.maxDate();

        public override double maxStrike() => volTS_.link.maxStrike();

        public override double minStrike() => volTS_.link.minStrike();

        protected override double blackVarianceImpl(double t, double strike) => volTS_.link.blackVariance(t, strike, true) + varianceOffset_;

        protected override double blackVolImpl(double t, double strike)
        {
            var nonZeroMaturity = t.IsEqual(0.0) ? 0.00001 : t;
            var var = blackVarianceImpl(nonZeroMaturity, strike);
            return System.Math.Sqrt(var / nonZeroMaturity);
        }
    }
}
