/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Cashflows
{
    //! CMS coupon class
    //    ! \warning This class does not perform any date adjustment,
    //                 i.e., the start and end date passed upon construction
    //                 should be already rolled to a business day.
    //
    [JetBrains.Annotations.PublicAPI] public class CmsCoupon : FloatingRateCoupon
    {
        // need by CashFlowVectors
        public CmsCoupon() { }

        public CmsCoupon(double nominal,
                         Date paymentDate,
                         Date startDate,
                         Date endDate,
                         int fixingDays,
                         SwapIndex swapIndex,
                         double gearing = 1.0,
                         double spread = 0.0,
                         Date refPeriodStart = null,
                         Date refPeriodEnd = null,
                         DayCounter dayCounter = null,
                         bool isInArrears = false)
           : base(paymentDate, nominal, startDate, endDate, fixingDays, swapIndex, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears)
        {
            swapIndex_ = swapIndex;
        }
        // Inspectors
        public SwapIndex swapIndex() => swapIndex_;

        private SwapIndex swapIndex_;

        // Factory - for Leg generators
        public override CashFlow factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays,
                                         InterestRateIndex index, double gearing, double spread,
                                         Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears) =>
            new CmsCoupon(nominal, paymentDate, startDate, endDate, fixingDays,
                (SwapIndex)index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears);
    }


    //! helper class building a sequence of capped/floored cms-rate coupons
}
