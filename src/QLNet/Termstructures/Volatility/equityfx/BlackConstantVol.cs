﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.equityfx
{
    //! Constant Black volatility, no time-strike dependence
    /*! This class implements the BlackVolatilityTermStructure interface for a constant Black volatility (no time/strike
        dependence). */
    [PublicAPI]
    public class BlackConstantVol : BlackVolatilityTermStructure
    {
        private Handle<Quote> volatility_;

        public BlackConstantVol(Date referenceDate, Calendar cal, double volatility, DayCounter dc)
            : base(referenceDate, cal, BusinessDayConvention.Following, dc)
        {
            volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
        }

        public BlackConstantVol(Date referenceDate, Calendar cal, Handle<Quote> volatility, DayCounter dc)
            : base(referenceDate, cal, BusinessDayConvention.Following, dc)
        {
            volatility_ = volatility;

            volatility_.registerWith(update);
        }

        public BlackConstantVol(int settlementDays, Calendar cal, double volatility, DayCounter dc)
            : base(settlementDays, cal, BusinessDayConvention.Following, dc)
        {
            volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
        }

        public BlackConstantVol(int settlementDays, Calendar cal, Handle<Quote> volatility, DayCounter dc)
            : base(settlementDays, cal, BusinessDayConvention.Following, dc)
        {
            volatility_ = volatility;

            volatility_.registerWith(update);
        }

        public override Date maxDate() => Date.maxDate();

        public override double maxStrike() => double.MaxValue;

        public override double minStrike() => double.MinValue;

        protected override double blackVolImpl(double t, double x) => volatility_.link.value();
    }
}
