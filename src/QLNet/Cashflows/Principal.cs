/*
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

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
    //! %principal payment over a fixed period
    //! This class implements part of the CashFlow interface but it is
    //  still abstract and provides derived classes with methods for accrual period calculations.
    [PublicAPI]
    public class Principal : CashFlow
    {
        protected double amount_;
        protected DayCounter dayCounter_;
        protected double nominal_;
        protected Date paymentDate_, accrualStartDate_, accrualEndDate_, refPeriodStart_, refPeriodEnd_;

        // Constructors
        public Principal()
        {
        } // default constructor

        public Principal(double amount,
            double nominal,
            Date paymentDate,
            Date accrualStartDate,
            Date accrualEndDate,
            DayCounter dayCounter,
            Date refPeriodStart = null,
            Date refPeriodEnd = null)
        {
            amount_ = amount;
            nominal_ = nominal;
            paymentDate_ = paymentDate;
            accrualStartDate_ = accrualStartDate;
            accrualEndDate_ = accrualEndDate;
            refPeriodStart_ = refPeriodStart;
            refPeriodEnd_ = refPeriodEnd;
            dayCounter_ = dayCounter;
            if (refPeriodStart_ == null)
            {
                refPeriodStart_ = accrualStartDate_;
            }

            if (refPeriodEnd_ == null)
            {
                refPeriodEnd_ = accrualEndDate_;
            }
        }

        public Date refPeriodEnd => refPeriodEnd_;

        public Date refPeriodStart => refPeriodStart_;

        public Date accrualEndDate() => accrualEndDate_;

        public Date accrualStartDate() => accrualStartDate_;

        public override double amount() => amount_;

        public override Date date() => paymentDate_;

        public DayCounter dayCounter() => dayCounter_;

        // access to properties
        public double nominal() => nominal_;

        public void setAmount(double amount)
        {
            amount_ = amount;
        }
    }
}
