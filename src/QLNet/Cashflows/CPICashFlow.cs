﻿using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class CPICashFlow : IndexedCashFlow
    {
        public CPICashFlow(double notional,
            ZeroInflationIndex index,
            Date baseDate,
            double baseFixing,
            Date fixingDate,
            Date paymentDate,
            bool growthOnly = false,
            InterpolationType interpolation = InterpolationType.AsIndex,
            Frequency frequency = Frequency.NoFrequency)
            : base(notional, index, baseDate, fixingDate, paymentDate, growthOnly)
        {
            baseFixing_ = baseFixing;
            interpolation_ = interpolation;
            frequency_ = frequency;

            Utils.QL_REQUIRE(System.Math.Abs(baseFixing_) > 1e-16, () => "|baseFixing|<1e-16, future divide-by-zero error");

            if (interpolation_ != InterpolationType.AsIndex)
            {
                Utils.QL_REQUIRE(frequency_ != Frequency.NoFrequency, () => "non-index interpolation w/o frequency");
            }
        }

        //! value used on base date
        /*! This does not have to agree with index on that date. */
        public virtual double baseFixing() => baseFixing_;

        //! you may not have a valid date
        public override Date baseDate()
        {
            Utils.QL_FAIL("no base date specified");
            return null;
        }

        //! do you want linear/constant/as-index interpolation of future data?
        public virtual InterpolationType interpolation() => interpolation_;

        public virtual Frequency frequency() => frequency_;

        //! redefined to use baseFixing() and interpolation
        public override double amount()
        {
            var I0 = baseFixing();
            double I1;

            // what interpolation do we use? Index / flat / linear
            if (interpolation() == InterpolationType.AsIndex)
            {
                I1 = index().fixing(fixingDate());
            }
            else
            {
                // work out what it should be
                var dd = Utils.inflationPeriod(fixingDate(), frequency());
                var indexStart = index().fixing(dd.Key);
                if (interpolation() == InterpolationType.Linear)
                {
                    var indexEnd = index().fixing(dd.Value + new Period(1, TimeUnit.Days));
                    // linear interpolation
                    I1 = indexStart + (indexEnd - indexStart) * (fixingDate() - dd.Key)
                        / (dd.Value + new Period(1, TimeUnit.Days) - dd.Key); // can't get to next period's value within current period
                }
                else
                {
                    // no interpolation, i.e. flat = constant, so use start-of-period value
                    I1 = indexStart;
                }

            }

            if (growthOnly())
                return notional() * (I1 / I0 - 1.0);
            else
                return notional() * (I1 / I0);
        }

        protected double baseFixing_;
        protected InterpolationType interpolation_;
        protected Frequency frequency_;
    }
}