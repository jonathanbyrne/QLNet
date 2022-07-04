//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using QLNet.Indexes.swap;
using QLNet.Time;

namespace QLNet.Cashflows
{
    /// <summary>
    /// CMS spread coupon class
    /// <remarks>
    /// This class does not perform any date adjustment,
    /// i.e., the start and end date passed upon construction
    /// should be already rolled to a business day.
    /// </remarks>
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class CmsSpreadCoupon : FloatingRateCoupon
    {
        // need by CashFlowVectors
        public CmsSpreadCoupon() { }

        public CmsSpreadCoupon(Date paymentDate,
                               double nominal,
                               Date startDate,
                               Date endDate,
                               int fixingDays,
                               SwapSpreadIndex index,
                               double gearing = 1.0,
                               double spread = 0.0,
                               Date refPeriodStart = null,
                               Date refPeriodEnd = null,
                               DayCounter dayCounter = null,
                               bool isInArrears = false)
           : base(paymentDate, nominal, startDate, endDate,
                  fixingDays, index, gearing, spread,
                  refPeriodStart, refPeriodEnd, dayCounter,
                  isInArrears)
        {
            index_ = index;
        }

        // Inspectors
        public SwapSpreadIndex swapSpreadIndex() => index_;

        private new SwapSpreadIndex index_;
    }
}
