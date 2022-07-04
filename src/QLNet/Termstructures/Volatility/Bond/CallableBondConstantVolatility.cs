/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using QLNet.Quotes;
using QLNet.Termstructures.Volatility;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.Bond
{
    //! Constant callable-bond volatility, no time-strike dependence
    [JetBrains.Annotations.PublicAPI] public class CallableBondConstantVolatility : CallableBondVolatilityStructure
    {
        public CallableBondConstantVolatility(Date referenceDate, double volatility, DayCounter dayCounter)
           : base(referenceDate)
        {
            volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
            dayCounter_ = dayCounter;
            maxBondTenor_ = new Period(100, TimeUnit.Years);
        }

        public CallableBondConstantVolatility(Date referenceDate, Handle<Quote> volatility, DayCounter dayCounter)
           : base(referenceDate)
        {
            volatility_ = volatility;
            dayCounter_ = dayCounter;
            maxBondTenor_ = new Period(100, TimeUnit.Years);
            volatility_.registerWith(update);
        }

        public CallableBondConstantVolatility(int settlementDays, Calendar calendar, double volatility, DayCounter dayCounter)
           : base(settlementDays, calendar)
        {
            volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
            dayCounter_ = dayCounter;
            maxBondTenor_ = new Period(100, TimeUnit.Years);
        }

        public CallableBondConstantVolatility(int settlementDays, Calendar calendar, Handle<Quote> volatility, DayCounter dayCounter)
           : base(settlementDays, calendar)
        {
            volatility_ = volatility;
            dayCounter_ = dayCounter;
            maxBondTenor_ = new Period(100, TimeUnit.Years);
            volatility_.registerWith(update);
        }

        // TermStructure interface
        public override DayCounter dayCounter() => dayCounter_;

        public override Date maxDate() => Date.maxDate();

        // CallableBondConstantVolatility interface
        public override Period maxBondTenor() => maxBondTenor_;

        public override double maxBondLength() => double.MaxValue;

        public override double minStrike() => double.MinValue;

        public override double maxStrike() => double.MaxValue;

        protected override double volatilityImpl(double d1, double d2, double d3) => volatility_.link.value();

        protected override SmileSection smileSectionImpl(double optionTime, double bondLength)
        {
            var atmVol = volatility_.link.value();
            return new FlatSmileSection(optionTime, atmVol, dayCounter_);
        }
        protected override double volatilityImpl(Date d, Period p, double d1) => volatility_.link.value();

        private Handle<Quote> volatility_;
        private DayCounter dayCounter_;
        private Period maxBondTenor_;
    }
}
