using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.Inflation
{
    [PublicAPI]
    public class ConstantYoYOptionletVolatility : YoYOptionletVolatilitySurface
    {
        protected double minStrike_, maxStrike_;
        protected double volatility_;

        // Constructor
        //! calculate the reference date based on the global evaluation date
        public ConstantYoYOptionletVolatility(double v,
            int settlementDays,
            Calendar cal,
            BusinessDayConvention bdc,
            DayCounter dc,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            double minStrike = -1.0, // -100%
            double maxStrike = 100.0) // +10,000%
            : base(settlementDays, cal, bdc, dc, observationLag, frequency, indexIsInterpolated)
        {
            volatility_ = v;
            minStrike_ = minStrike;
            maxStrike_ = maxStrike;
        }

        // Limits
        public override Date maxDate() => Date.maxDate();

        //! the maximum strike for which the term structure can return vols
        public override double maxStrike() => maxStrike_;

        //! the minimum strike for which the term structure can return vols
        public override double minStrike() => minStrike_;

        //! implements the actual volatility calculation in derived classes
        protected override double volatilityImpl(double length, double strike) => volatility_;
    }
}
