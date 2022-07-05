/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

using QLNet.Time;

namespace QLNet.Cashflows
{
    //! %coupon accruing over a fixed period
    //! This class implements part of the CashFlow interface but it is
    //  still abstract and provides derived classes with methods for accrual period calculations.
    public abstract class Coupon : CashFlow
    {
        protected Date accrualEndDate_;
        protected double? accrualPeriod_;
        protected Date accrualStartDate_;
        protected Date exCouponDate_;
        protected double nominal_;
        protected Date paymentDate_;
        protected Date refPeriodEnd_;
        protected Date refPeriodStart_;

        // Constructors
        protected Coupon()
        {
        } // default constructor

        // coupon does not adjust the payment date which must already be a business day
        protected Coupon(Date paymentDate,
            double nominal,
            Date accrualStartDate,
            Date accrualEndDate,
            Date refPeriodStart = null,
            Date refPeriodEnd = null,
            Date exCouponDate = null)
        {
            paymentDate_ = paymentDate;
            nominal_ = nominal;
            accrualStartDate_ = accrualStartDate;
            accrualEndDate_ = accrualEndDate;
            refPeriodStart_ = refPeriodStart;
            refPeriodEnd_ = refPeriodEnd;
            exCouponDate_ = exCouponDate;
            accrualPeriod_ = null;

            if (refPeriodStart_ == null)
            {
                refPeriodStart_ = accrualStartDate_;
            }

            if (refPeriodEnd_ == null)
            {
                refPeriodEnd_ = accrualEndDate_;
            }
        }

        //! end date of the reference period
        public Date referencePeriodEnd => refPeriodEnd_;

        //! start date of the reference period
        public Date referencePeriodStart => refPeriodStart_;

        //! accrued amount at the given date
        public abstract double accruedAmount(Date d);

        //! day counter for accrual calculation
        public abstract DayCounter dayCounter();

        //! accrued rate
        public abstract double rate();

        //! accrual period in days
        public int accrualDays() => dayCounter().dayCount(accrualStartDate_, accrualEndDate_);

        //! end of the accrual period
        public Date accrualEndDate() => accrualEndDate_;

        //! accrual period as fraction of year
        public double accrualPeriod()
        {
            if (accrualPeriod_ == null)
            {
                accrualPeriod_ = dayCounter().yearFraction(accrualStartDate_,
                    accrualEndDate_, refPeriodStart_, refPeriodEnd_);
            }

            return accrualPeriod_.Value;
        }

        //! start of the accrual period
        public Date accrualStartDate() => accrualStartDate_;

        //! accrued days at the given date
        public int accruedDays(Date d)
        {
            if (d <= accrualStartDate_ || d > paymentDate_)
            {
                return 0;
            }

            return dayCounter().dayCount(accrualStartDate_, Date.Min(d, accrualEndDate_));
        }

        //! accrued period as fraction of year at the given date
        public double accruedPeriod(Date d)
        {
            if (d <= accrualStartDate_ || d > paymentDate_)
            {
                return 0.0;
            }

            return dayCounter().yearFraction(accrualStartDate_,
                Date.Min(d, accrualEndDate_),
                refPeriodStart_,
                refPeriodEnd_);
        }

        // Event interface
        public override Date date() => paymentDate_;

        // CashFlow interface
        public override Date exCouponDate() => exCouponDate_;

        // Inspectors
        public double nominal() => nominal_;
    }
}
