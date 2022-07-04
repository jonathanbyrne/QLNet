using QLNet.Time;

namespace QLNet.Termstructures.Volatility.Inflation
{
    [JetBrains.Annotations.PublicAPI] public class ConstantYoYOptionletVolatility : YoYOptionletVolatilitySurface
    {

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
            double minStrike = -1.0,  // -100%
            double maxStrike = 100.0)  // +10,000%
            : base(settlementDays, cal, bdc, dc, observationLag, frequency, indexIsInterpolated)
        {
            volatility_ = v;
            minStrike_ = minStrike;
            maxStrike_ = maxStrike;
        }

        // Limits
        public override Date maxDate() => Date.maxDate();

        //! the minimum strike for which the term structure can return vols
        public override double minStrike() => minStrike_;

        //! the maximum strike for which the term structure can return vols
        public override double maxStrike() => maxStrike_;

        //! implements the actual volatility calculation in derived classes
        protected override double volatilityImpl(double length, double strike) => volatility_;

        protected double volatility_;
        protected double minStrike_, maxStrike_;
    }
}