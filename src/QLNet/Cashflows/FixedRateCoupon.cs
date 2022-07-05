/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)

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
    //! %Coupon paying a fixed interest rate
    [PublicAPI]
    public class FixedRateCoupon : Coupon
    {
        private double? amount_;
        private InterestRate rate_;

        // constructors
        public FixedRateCoupon(Date paymentDate, double nominal, double rate, DayCounter dayCounter,
            Date accrualStartDate, Date accrualEndDate,
            Date refPeriodStart = null, Date refPeriodEnd = null, Date exCouponDate = null)
            : base(paymentDate, nominal, accrualStartDate, accrualEndDate, refPeriodStart, refPeriodEnd, exCouponDate)
        {
            rate_ = new InterestRate(rate, dayCounter, Compounding.Simple, Frequency.Annual);
        }

        public FixedRateCoupon(Date paymentDate, double nominal, InterestRate interestRate,
            Date accrualStartDate, Date accrualEndDate,
            Date refPeriodStart = null, Date refPeriodEnd = null, Date exCouponDate = null, double? amount = null)
            : base(paymentDate, nominal, accrualStartDate, accrualEndDate, refPeriodStart, refPeriodEnd, exCouponDate)
        {
            amount_ = amount;
            rate_ = interestRate;
        }

        public override double accruedAmount(Date d)
        {
            if (d <= accrualStartDate_ || d > paymentDate_)
            {
                return 0;
            }

            if (tradingExCoupon(d))
            {
                return -nominal() * (rate_.compoundFactor(d,
                    accrualEndDate_,
                    refPeriodStart_,
                    refPeriodEnd_) - 1.0);
            }

            return nominal() * (rate_.compoundFactor(accrualStartDate_, Date.Min(d, accrualEndDate_),
                refPeriodStart_, refPeriodEnd_) - 1.0);
        }

        //! CashFlow interface
        public override double amount()
        {
            if (amount_ != null)
            {
                return amount_.Value;
            }

            return nominal() * (rate_.compoundFactor(accrualStartDate_, accrualEndDate_, refPeriodStart_, refPeriodEnd_) - 1.0);
        }

        public override DayCounter dayCounter() => rate_.dayCounter();

        public InterestRate interestRate() => rate_;

        //! Coupon interface
        public override double rate() => rate_.rate();
    }

    //! helper class building a sequence of fixed rate coupons
}
