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

using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Time;
using System;
using System.Collections.Generic;
using QLNet.Time.DayCounters;

namespace QLNet.Cashflows
{
    //! when you observe an index, how do you interpolate between fixings?
    public enum InterpolationType
    {
        AsIndex,   //!< same interpolation as index
        Flat,      //!< flat from previous fixing
        Linear     //!< linearly between bracketing fixings
    }

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
    [JetBrains.Annotations.PublicAPI] public class CPICoupon : InflationCoupon
    {
        protected double baseCPI_;
        protected double fixedRate_;
        protected double spread_;
        protected InterpolationType observationInterpolation_;

        protected override bool checkPricerImpl(InflationCouponPricer pricer)
        {
            var p = pricer as CPICouponPricer;
            return p != null;
        }

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

        // Inspectors
        // fixed rate that will be inflated by the index ratio
        public double fixedRate() => fixedRate_;

        //! spread paid over the fixing of the underlying index
        public double spread() => spread_;

        //! adjusted fixing (already divided by the base fixing)
        public double adjustedFixing() => (rate() - spread()) / fixedRate();

        //! allows for a different interpolation from the index
        public override double indexFixing() => indexFixing(fixingDate());

        //! base value for the CPI index
        /*! \warning make sure that the interpolation used to create
                    this is what you are using for the fixing,
                    i.e. the observationInterpolation.
        */
        public double baseCPI() => baseCPI_;

        //! how do you observe the index?  as-is, flat, linear?
        public InterpolationType observationInterpolation() => observationInterpolation_;

        //! utility method, calls indexFixing
        public double indexObservation(Date onDate) => indexFixing(onDate);

        //! index used
        public ZeroInflationIndex cpiIndex() => index() as ZeroInflationIndex;
    }

    //! Cash flow paying the performance of a CPI (zero inflation) index
    /*! It is NOT a coupon, i.e. no accruals. */
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

    //! Helper class building a sequence of capped/floored CPI coupons.
    /*! Also allowing for the inflated notional at the end...
        especially if there is only one date in the schedule.
        If a fixedRate is zero you get a FixedRateCoupon, otherwise
        you get a ZeroInflationCoupon.

        payoff is: spread + fixedRate x index
    */
    [JetBrains.Annotations.PublicAPI] public class CPILeg : CPILegBase
    {
        public CPILeg(Schedule schedule,
                      ZeroInflationIndex index,
                      double baseCPI,
                      Period observationLag)
        {
            schedule_ = schedule;
            index_ = index;
            baseCPI_ = baseCPI;
            observationLag_ = observationLag;
            paymentDayCounter_ = new Thirty360();
            paymentAdjustment_ = BusinessDayConvention.ModifiedFollowing;
            paymentCalendar_ = schedule.calendar();
            fixingDays_ = new List<int>() { 0 };
            observationInterpolation_ = InterpolationType.AsIndex;
            subtractInflationNominal_ = true;
            spreads_ = new List<double>() { 0 };
        }

        public override List<CashFlow> value()
        {
            Utils.QL_REQUIRE(!notionals_.empty(), () => "no notional given");

            var n = schedule_.Count - 1;
            var leg = new List<CashFlow>(n + 1);

            if (n > 0)
            {
                Utils.QL_REQUIRE(!fixedRates_.empty() || !spreads_.empty(), () => "no fixedRates or spreads given");

                Date refStart, start, refEnd, end;

                for (var i = 0; i < n; ++i)
                {
                    refStart = start = schedule_.date(i);
                    refEnd = end = schedule_.date(i + 1);
                    var paymentDate = paymentCalendar_.adjust(end, paymentAdjustment_);

                    Date exCouponDate = null;
                    if (exCouponPeriod_ != null)
                    {
                        exCouponDate = exCouponCalendar_.advance(paymentDate,
                                                                 -exCouponPeriod_,
                                                                 exCouponAdjustment_,
                                                                 exCouponEndOfMonth_);
                    }

                    if (i == 0 && !schedule_.isRegular(i + 1))
                    {
                        var bdc = schedule_.businessDayConvention();
                        refStart = schedule_.calendar().adjust(end - schedule_.tenor(), bdc);
                    }
                    if (i == n - 1 && !schedule_.isRegular(i + 1))
                    {
                        var bdc = schedule_.businessDayConvention();
                        refEnd = schedule_.calendar().adjust(start + schedule_.tenor(), bdc);
                    }
                    if (fixedRates_.Get(i, 1.0).IsEqual(0.0))
                    {
                        // fixed coupon
                        leg.Add(new FixedRateCoupon(paymentDate, notionals_.Get(i, 0.0),
                                                    Utils.effectiveFixedRate(spreads_, caps_, floors_, i),
                                                    paymentDayCounter_, start, end, refStart, refEnd, exCouponDate));
                    }
                    else
                    {
                        // zero inflation coupon
                        if (Utils.noOption(caps_, floors_, i))
                        {
                            // just swaplet
                            CPICoupon coup;

                            coup = new CPICoupon(baseCPI_,    // all have same base for ratio
                                                 paymentDate,
                                                 notionals_.Get(i, 0.0),
                                                 start, end,
                                                 fixingDays_.Get(i, 0),
                                                 index_, observationLag_,
                                                 observationInterpolation_,
                                                 paymentDayCounter_,
                                                 fixedRates_.Get(i, 0.0),
                                                 spreads_.Get(i, 0.0),
                                                 refStart, refEnd, exCouponDate);

                            // in this case you can set a pricer
                            // straight away because it only provides computation - not data
                            var pricer = new CPICouponPricer();
                            coup.setPricer(pricer);
                            leg.Add(coup);

                        }
                        else
                        {
                            // cap/floorlet
                            Utils.QL_FAIL("caps/floors on CPI coupons not implemented.");
                        }
                    }
                }
            }

            // in CPI legs you always have a notional flow of some sort
            var pDate = paymentCalendar_.adjust(schedule_.date(n), paymentAdjustment_);
            var fixingDate = pDate - observationLag_;
            CashFlow xnl = new CPICashFlow
            (notionals_.Get(n, 0.0), index_,
             new Date(), // is fake, i.e. you do not have one
             baseCPI_, fixingDate, pDate,
             subtractInflationNominal_, observationInterpolation_,
             index_.frequency());

            leg.Add(xnl);

            return leg;
        }
    }
}
