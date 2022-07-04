/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Indexes;
using QLNet.Time;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Cashflows
{
    /// <summary>
    /// Average BMA coupon
    /// <para>Coupon paying a BMA index, where the coupon rate is a
    /// weighted average of relevant fixings.</para>
    /// </summary>
    /// <remarks>
    /// The weighted average is computed based on the
    /// actual calendar days for which a given fixing is valid and
    /// contributing to the given interest period.
    ///
    /// Before weights are computed, the fixing schedule is adjusted
    /// for the index's fixing day gap. See rate() method for details.
    /// </remarks>
    [JetBrains.Annotations.PublicAPI] public class AverageBmaCoupon : FloatingRateCoupon
    {

        public AverageBmaCoupon(Date paymentDate,
                                double nominal,
                                Date startDate,
                                Date endDate,
                                BMAIndex index,
                                double gearing = 1.0,
                                double spread = 0.0,
                                Date refPeriodStart = null,
                                Date refPeriodEnd = null,
                                DayCounter dayCounter = null)
           : base(paymentDate, nominal, startDate, endDate, index.fixingDays(), index, gearing, spread,
                  refPeriodStart, refPeriodEnd, dayCounter)
        {
            fixingSchedule_ = index.fixingSchedule(
                                 index.fixingCalendar()
                                 .advance(startDate, new Period(-index.fixingDays(), TimeUnit.Days),
                                          BusinessDayConvention.Preceding), endDate);
            setPricer(new AverageBmaCouponPricer());
        }

        /// <summary>
        /// Get the fixing date
        /// </summary>
        /// <remarks>FloatingRateCoupon interface not applicable here; use <c>fixingDates()</c> instead
        /// </remarks>
        public override Date fixingDate()
        {
            Utils.QL_FAIL("no single fixing date for average-BMA coupon");
            return null;
        }

        /// <summary>
        /// Get the fixing dates of the rates to be averaged
        /// </summary>
        /// <returns>A list of dates</returns>
        public List<Date> FixingDates() => fixingSchedule_.dates();

        /// <summary>
        /// not applicable here; use indexFixings() instead
        /// </summary>
        public override double indexFixing()
        {
            Utils.QL_FAIL("no single fixing for average-BMA coupon");
            return 0;
        }

        /// <summary>
        /// fixings of the underlying index to be averaged
        /// </summary>
        /// <returns>A list of double</returns>
        public List<double> IndexFixings() { return fixingSchedule_.dates().Select(d => index_.fixing(d)).ToList(); }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double convexityAdjustment()
        {
            Utils.QL_FAIL("not defined for average-BMA coupon");
            return 0;
        }

        private Schedule fixingSchedule_;
    }

    [JetBrains.Annotations.PublicAPI] public class AverageBmaCouponPricer : FloatingRateCouponPricer
    {
        public override void initialize(FloatingRateCoupon coupon)
        {
            coupon_ = coupon as AverageBmaCoupon;
            Utils.QL_REQUIRE(coupon_ != null, () => "wrong coupon ExerciseType");
        }

        public override double swapletRate()
        {
            var fixingDates = coupon_.FixingDates();
            var index = coupon_.index();

            var cutoffDays = 0; // to be verified
            Date startDate = coupon_.accrualStartDate() - cutoffDays,
                 endDate = coupon_.accrualEndDate() - cutoffDays,
                 d1 = startDate;

            Utils.QL_REQUIRE(fixingDates.Count > 0, () => "fixing date list empty");
            Utils.QL_REQUIRE(index.valueDate(fixingDates.First()) <= startDate, () => "first fixing date valid after period start");
            Utils.QL_REQUIRE(index.valueDate(fixingDates.Last()) >= endDate, () => "last fixing date valid before period end");

            var avgBma = 0.0;
            var days = 0;
            for (var i = 0; i < fixingDates.Count - 1; ++i)
            {
                var valueDate = index.valueDate(fixingDates[i]);
                var nextValueDate = index.valueDate(fixingDates[i + 1]);

                if (fixingDates[i] >= endDate || valueDate >= endDate)
                    break;
                if (fixingDates[i + 1] < startDate || nextValueDate <= startDate)
                    continue;

                var d2 = Date.Min(nextValueDate, endDate);

                avgBma += index.fixing(fixingDates[i]) * (d2 - d1);

                days += d2 - d1;
                d1 = d2;
            }
            avgBma /= endDate - startDate;

            Utils.QL_REQUIRE(days == endDate - startDate, () =>
                             "averaging days " + days + " differ from " + "interest days " + (endDate - startDate));

            return coupon_.gearing() * avgBma + coupon_.spread();
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double swapletPrice()
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double capletPrice(double d)
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double capletRate(double d)
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double floorletPrice(double d)
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double floorletRate(double d)
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        // recheck
        //protected override double optionletPrice( QLNet.Option.Type t, double d )
        //{
        //   throw new Exception( "not available" );
        //}

        private AverageBmaCoupon coupon_;
    }

    /// <summary>
    /// Helper class building a sequence of average BMA coupons
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class AverageBmaLeg : RateLegBase
    {
        private BMAIndex index_;
        private List<double> gearings_;
        private List<double> spreads_;

        public AverageBmaLeg(Schedule schedule, BMAIndex index)
        {
            schedule_ = schedule;
            index_ = index;
            paymentAdjustment_ = BusinessDayConvention.Following;
        }

        public AverageBmaLeg WithPaymentDayCounter(DayCounter dayCounter)
        {
            paymentDayCounter_ = dayCounter;
            return this;
        }
        public AverageBmaLeg WithGearings(double gearing)
        {
            gearings_ = new List<double>() { gearing };
            return this;
        }
        public AverageBmaLeg WithGearings(List<double> gearings)
        {
            gearings_ = gearings;
            return this;
        }
        public AverageBmaLeg WithSpreads(double spread)
        {
            spreads_ = new List<double>() { spread };
            return this;
        }
        public AverageBmaLeg WithSpreads(List<double> spreads)
        {
            spreads_ = spreads;
            return this;
        }

        public override List<CashFlow> value()
        {
            Utils.QL_REQUIRE(!notionals_.empty(), () => "no notional given");

            var cashflows = new List<CashFlow>();

            // the following is not always correct
            var calendar = schedule_.calendar();

            Date refStart, start, refEnd, end;
            Date paymentDate;

            var n = schedule_.Count - 1;
            for (var i = 0; i < n; ++i)
            {
                refStart = start = schedule_.date(i);
                refEnd = end = schedule_.date(i + 1);
                paymentDate = calendar.adjust(end, paymentAdjustment_);
                if (i == 0 && !schedule_.isRegular(i + 1))
                    refStart = calendar.adjust(end - schedule_.tenor(), paymentAdjustment_);
                if (i == n - 1 && !schedule_.isRegular(i + 1))
                    refEnd = calendar.adjust(start + schedule_.tenor(), paymentAdjustment_);

                cashflows.Add(new AverageBmaCoupon(paymentDate,
                                                   notionals_.Get(i, notionals_.Last()),
                                                   start, end,
                                                   index_,
                                                   gearings_.Get(i, 1.0),
                                                   spreads_.Get(i, 0.0),
                                                   refStart, refEnd,
                                                   paymentDayCounter_));
            }

            return cashflows;
        }
    }
}
