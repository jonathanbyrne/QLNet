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

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Cashflows
{
    /// <summary>
    ///     Average BMA coupon
    ///     <para>
    ///         Coupon paying a BMA index, where the coupon rate is a
    ///         weighted average of relevant fixings.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     The weighted average is computed based on the
    ///     actual calendar days for which a given fixing is valid and
    ///     contributing to the given interest period.
    ///     Before weights are computed, the fixing schedule is adjusted
    ///     for the index's fixing day gap. See rate() method for details.
    /// </remarks>
    [PublicAPI]
    public class AverageBmaCoupon : FloatingRateCoupon
    {
        private Schedule fixingSchedule_;

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
        ///     not applicable here
        /// </summary>
        public override double convexityAdjustment()
        {
            Utils.QL_FAIL("not defined for average-BMA coupon");
            return 0;
        }

        /// <summary>
        ///     Get the fixing date
        /// </summary>
        /// <remarks>
        ///     FloatingRateCoupon interface not applicable here; use <c>fixingDates()</c> instead
        /// </remarks>
        public override Date fixingDate()
        {
            Utils.QL_FAIL("no single fixing date for average-BMA coupon");
            return null;
        }

        /// <summary>
        ///     Get the fixing dates of the rates to be averaged
        /// </summary>
        /// <returns>A list of dates</returns>
        public List<Date> FixingDates() => fixingSchedule_.dates();

        /// <summary>
        ///     not applicable here; use indexFixings() instead
        /// </summary>
        public override double indexFixing()
        {
            Utils.QL_FAIL("no single fixing for average-BMA coupon");
            return 0;
        }

        /// <summary>
        ///     fixings of the underlying index to be averaged
        /// </summary>
        /// <returns>A list of double</returns>
        public List<double> IndexFixings()
        {
            return fixingSchedule_.dates().Select(d => index_.fixing(d)).ToList();
        }
    }
}
