/*
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

namespace QLNet.Termstructures.Volatility.Optionlet
{
    //! Constant caplet volatility, no time-strike dependence
    [PublicAPI]
    public class ConstantOptionletVolatility : OptionletVolatilityStructure
    {
        private Handle<Quote> volatility_;

        //! floating reference date, floating market data
        public ConstantOptionletVolatility(int settlementDays, Calendar cal, BusinessDayConvention bdc,
            Handle<Quote> vol, DayCounter dc)
            : base(settlementDays, cal, bdc, dc)
        {
            volatility_ = vol;

            volatility_.registerWith(update);
        }

        //! fixed reference date, floating market data
        public ConstantOptionletVolatility(Date referenceDate, Calendar cal, BusinessDayConvention bdc,
            Handle<Quote> vol, DayCounter dc)
            : base(referenceDate, cal, bdc, dc)
        {
            volatility_ = vol;

            volatility_.registerWith(update);
        }

        //! floating reference date, fixed market data
        public ConstantOptionletVolatility(int settlementDays, Calendar cal, BusinessDayConvention bdc,
            double vol, DayCounter dc)
            : base(settlementDays, cal, bdc, dc)
        {
            volatility_ = new Handle<Quote>(new SimpleQuote(vol));
        }

        //! fixed reference date, fixed market data
        public ConstantOptionletVolatility(Date referenceDate, Calendar cal, BusinessDayConvention bdc,
            double vol, DayCounter dc)
            : base(referenceDate, cal, bdc, dc)
        {
            volatility_ = new Handle<Quote>(new SimpleQuote(vol));
        }

        public override Date maxDate() => Date.maxDate();

        public override double maxStrike() => double.MaxValue;

        public override double minStrike() => double.MinValue;

        protected override SmileSection smileSectionImpl(Date d)
        {
            var atmVol = volatility_.link.value();
            return new FlatSmileSection(d, atmVol, dayCounter(), referenceDate());
        }

        protected override SmileSection smileSectionImpl(double optionTime)
        {
            var atmVol = volatility_.link.value();
            return new FlatSmileSection(optionTime, atmVol, dayCounter());
        }

        protected override double volatilityImpl(double d1, double d2) => volatility_.link.value();
    }
}
