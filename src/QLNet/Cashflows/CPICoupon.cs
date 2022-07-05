/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    //! when you observe an index, how do you interpolate between fixings?

    //! %Coupon paying the performance of a CPI (zero inflation) index
    /*! The performance is relative to the index value on the base date.

       The other inflation value is taken from the refPeriodEnd date
       with observation lag, so any roll/calendar etc. will be built
       in by the caller.  By default this is done in the
       InflationCoupon which uses ModifiedPreceding with fixing days
       assumed positive meaning earlier, i.e. always stay in same
       month (relative to referencePeriodEnd).

       This is more sophisticated than an %IndexedCashFlow because it
       does date calculations itself.

       We do not do any convexity adjustment for lags different
       to the natural ZCIIS lag that was used to create the
       forward inflation curve.
    */
    [PublicAPI]
    public class CPICoupon : InflationCoupon
    {
        protected double baseCPI_;
        protected double fixedRate_;
        protected InterpolationType observationInterpolation_;
        protected double spread_;

        public CPICoupon(double baseCPI, // user provided, could be arbitrary
            Date paymentDate,
            double nominal,
            Date startDate,
            Date endDate,
            int fixingDays,
            ZeroInflationIndex index,
            Period observationLag,
            InterpolationType observationInterpolation,
            DayCounter dayCounter,
            double fixedRate, // aka gearing
            double spread = 0.0,
            Date refPeriodStart = null,
            Date refPeriodEnd = null,
            Date exCouponDate = null)
            : base(paymentDate, nominal, startDate, endDate, fixingDays, index,
                observationLag, dayCounter, refPeriodStart, refPeriodEnd, exCouponDate)
        {
            baseCPI_ = baseCPI;
            fixedRate_ = fixedRate;
            spread_ = spread;
            observationInterpolation_ = observationInterpolation;
            Utils.QL_REQUIRE(System.Math.Abs(baseCPI_) > 1e-16, () => "|baseCPI_| < 1e-16, future divide-by-zero problem");
        }

        //! adjusted fixing (already divided by the base fixing)
        public double adjustedFixing() => (rate() - spread()) / fixedRate();

        //! base value for the CPI index
        /*! \warning make sure that the interpolation used to create
                    this is what you are using for the fixing,
                    i.e. the observationInterpolation.
        */
        public double baseCPI() => baseCPI_;

        //! index used
        public ZeroInflationIndex cpiIndex() => index() as ZeroInflationIndex;

        // Inspectors
        // fixed rate that will be inflated by the index ratio
        public double fixedRate() => fixedRate_;

        //! allows for a different interpolation from the index
        public override double indexFixing() => indexFixing(fixingDate());

        //! utility method, calls indexFixing
        public double indexObservation(Date onDate) => indexFixing(onDate);

        //! how do you observe the index?  as-is, flat, linear?
        public InterpolationType observationInterpolation() => observationInterpolation_;

        //! spread paid over the fixing of the underlying index
        public double spread() => spread_;

        protected override bool checkPricerImpl(InflationCouponPricer pricer) => pricer is CPICouponPricer p;

        // use to calculate for fixing date, allows change of
        // interpolation w.r.t. index.  Can also be used ahead of time
        protected double indexFixing(Date d)
        {
            // you may want to modify the interpolation of the index
            // this gives you the chance

            double I1;
            // what interpolation do we use? Index / flat / linear
            if (observationInterpolation() == InterpolationType.AsIndex)
            {
                I1 = cpiIndex().fixing(d);
            }
            else
            {
                // work out what it should be
                var dd = Utils.inflationPeriod(d, cpiIndex().frequency());
                var indexStart = cpiIndex().fixing(dd.Key);
                if (observationInterpolation() == InterpolationType.Linear)
                {
                    var indexEnd = cpiIndex().fixing(dd.Value + new Period(1, TimeUnit.Days));
                    // linear interpolation
                    I1 = indexStart + (indexEnd - indexStart) * (d - dd.Key)
                        / (dd.Value + new Period(1, TimeUnit.Days) - dd.Key); // can't get to next period's value within current period
                }
                else
                {
                    // no interpolation, i.e. flat = constant, so use start-of-period value
                    I1 = indexStart;
                }
            }

            return I1;
        }
    }

    //! Cash flow paying the performance of a CPI (zero inflation) index
    /*! It is NOT a coupon, i.e. no accruals. */

    //! Helper class building a sequence of capped/floored CPI coupons.
    /*! Also allowing for the inflated notional at the end...
        especially if there is only one date in the schedule.
        If a fixedRate is zero you get a FixedRateCoupon, otherwise
        you get a ZeroInflationCoupon.

        payoff is: spread + fixedRate x index
    */
}
