/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 *
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
using QLNet.Time;

namespace QLNet.Cashflows
{
    //! Cash flow dependent on an index ratio.

    /*! This cash flow is not a coupon, i.e., there's no accrual.  The
        amount is either i(T)/i(0) or i(T)/i(0) - 1, depending on the
        growthOnly parameter.

        We expect this to be used inside an instrument that does all the date
        adjustment etc., so this takes just dates and does not change them.
        growthOnly = false means i(T)/i(0), which is a bond-ExerciseType setting.
        growthOnly = true means i(T)/i(0) - 1, which is a swap-ExerciseType setting.
    */
    [PublicAPI]
    public class IndexedCashFlow : CashFlow
    {
        private Date baseDate_, fixingDate_, paymentDate_;
        private bool growthOnly_;
        private Index index_;
        private double notional_;

        public IndexedCashFlow(double notional,
            Index index,
            Date baseDate,
            Date fixingDate,
            Date paymentDate,
            bool growthOnly = false)
        {
            notional_ = notional;
            index_ = index;
            baseDate_ = baseDate;
            fixingDate_ = fixingDate;
            paymentDate_ = paymentDate;
            growthOnly_ = growthOnly;
        }

        public override double amount()
        {
            var I0 = index_.fixing(baseDate_);
            var I1 = index_.fixing(fixingDate_);

            if (growthOnly_)
            {
                return notional_ * (I1 / I0 - 1.0);
            }

            return notional_ * (I1 / I0);
        }

        public virtual Date baseDate() => baseDate_;

        public override Date date() => paymentDate_;

        public virtual Date fixingDate() => fixingDate_;

        public virtual bool growthOnly() => growthOnly_;

        public virtual Index index() => index_;

        public virtual double notional() => notional_;
    }
}
