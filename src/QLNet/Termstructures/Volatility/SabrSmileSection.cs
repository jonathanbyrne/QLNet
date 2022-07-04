using System.Collections.Generic;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;
using QLNet.Time.DayCounters;

namespace QLNet.Termstructures.Volatility
{
    [JetBrains.Annotations.PublicAPI] public class SabrSmileSection : SmileSection
    {
        private double alpha_, beta_, nu_, rho_, forward_, shift_;
        private VolatilityType volatilityType_;

        public SabrSmileSection(double timeToExpiry, double forward, List<double> sabrParams, VolatilityType volatilityType = VolatilityType.ShiftedLognormal, double shift = 0.0)
            : base(timeToExpiry, null, volatilityType, shift)
        {
            forward_ = forward;
            shift_ = shift;
            volatilityType_ = volatilityType;

            alpha_ = sabrParams[0];
            beta_ = sabrParams[1];
            nu_ = sabrParams[2];
            rho_ = sabrParams[3];

            Utils.QL_REQUIRE(volatilityType == VolatilityType.Normal || forward_ + shift_ > 0.0, () => "at the money forward rate + shift must be: " + forward_ + shift_ + " not allowed");
            Utils.validateSabrParameters(alpha_, beta_, nu_, rho_);
        }

        public SabrSmileSection(Date d, double forward, List<double> sabrParams, DayCounter dc = null, VolatilityType volatilityType = VolatilityType.ShiftedLognormal, double shift = 0.0)
            : base(d, dc ?? new Actual365Fixed(), null, volatilityType, shift)
        {
            forward_ = forward;
            shift_ = shift;
            volatilityType_ = volatilityType;

            alpha_ = sabrParams[0];
            beta_ = sabrParams[1];
            nu_ = sabrParams[2];
            rho_ = sabrParams[3];

            Utils.QL_REQUIRE(volatilityType == VolatilityType.Normal || forward_ + shift_ > 0.0, () => "at the money forward rate +shift must be: " + forward_ + shift_ + " not allowed");
            Utils.validateSabrParameters(alpha_, beta_, nu_, rho_);
        }

        public override double minStrike() => 0.0;

        public override double maxStrike() => double.MaxValue;

        public override double? atmLevel() => forward_;

        protected override double varianceImpl(double strike)
        {
            double vol;
            if (volatilityType_ == VolatilityType.ShiftedLognormal)
                vol = Utils.shiftedSabrVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_, shift_);
            else
                vol = Utils.shiftedSabrNormalVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_, shift_);

            return vol * vol * exerciseTime();
        }

        protected override double volatilityImpl(double strike)
        {
            double vol;
            if (volatilityType_ == VolatilityType.ShiftedLognormal)
                vol = Utils.shiftedSabrVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_, shift_);
            else
                vol = Utils.shiftedSabrNormalVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_, shift_);

            return vol;
        }
    }
}