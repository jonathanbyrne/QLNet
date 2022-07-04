/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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
using QLNet.Time;
using System;

namespace QLNet.Cashflows
{
    //! Predetermined cash flow
    /*! This cash flow pays a predetermined amount at a given date. */
    [JetBrains.Annotations.PublicAPI] public class SimpleCashFlow : CashFlow
    {
        private double amount_;
        public override double amount() => amount_;

        private Date date_;
        public override Date date() => date_;

        public SimpleCashFlow(double amount, Date date)
        {
            Utils.QL_REQUIRE(date != null, () => "null date SimpleCashFlow");
            amount_ = amount;
            date_ = date;
        }
    }

    //! Bond redemption
    /*! This class specializes SimpleCashFlow so that visitors
        can perform more detailed cash-flow analysis.
    */

    //! Amortizing payment
    /*! This class specializes SimpleCashFlow so that visitors
        can perform more detailed cash-flow analysis.
    */

    //! Voluntary Prepay
    /*! This class specializes SimpleCashFlow so that visitors
        can perform more detailed cash-flow analysis.
    */
}
