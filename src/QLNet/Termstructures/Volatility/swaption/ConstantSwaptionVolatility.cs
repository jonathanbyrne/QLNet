/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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

using System;
using JetBrains.Annotations;
using QLNet.Quotes;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.swaption
{
    //! Constant swaption volatility, no time-strike dependence
    [PublicAPI]
    public class ConstantSwaptionVolatility : SwaptionVolatilityStructure
    {
        private Period maxSwapTenor_;
        private double? shift_;
        private Handle<Quote> volatility_;
        private VolatilityType volatilityType_;

        //! floating reference date, floating market data
        public ConstantSwaptionVolatility(int settlementDays,
            Calendar cal,
            BusinessDayConvention bdc,
            Handle<Quote> vol,
            DayCounter dc,
            VolatilityType type = VolatilityType.ShiftedLognormal,
            double? shift = 0.0)
            : base(settlementDays, cal, bdc, dc)
        {
            volatility_ = vol;
            maxSwapTenor_ = new Period(100, TimeUnit.Years);
            volatilityType_ = type;
            shift_ = shift;
            volatility_.registerWith(update);
        }

        //! fixed reference date, floating market data
        public ConstantSwaptionVolatility(Date referenceDate,
            Calendar cal,
            BusinessDayConvention bdc,
            Handle<Quote> vol,
            DayCounter dc,
            VolatilityType type = VolatilityType.ShiftedLognormal,
            double? shift = 0.0)
            : base(referenceDate, cal, bdc, dc)
        {
            volatility_ = vol;
            maxSwapTenor_ = new Period(100, TimeUnit.Years);
            volatilityType_ = type;
            shift_ = shift;
            volatility_.registerWith(update);
        }

        //! floating reference date, fixed market data
        public ConstantSwaptionVolatility(int settlementDays,
            Calendar cal,
            BusinessDayConvention bdc,
            double vol,
            DayCounter dc,
            VolatilityType type = VolatilityType.ShiftedLognormal,
            double? shift = 0.0)
            : base(settlementDays, cal, bdc, dc)
        {
            volatility_ = new Handle<Quote>(new SimpleQuote(vol));
            maxSwapTenor_ = new Period(100, TimeUnit.Years);
            volatilityType_ = type;
            shift_ = shift;
        }

        //! fixed reference date, fixed market data
        public ConstantSwaptionVolatility(Date referenceDate,
            Calendar cal,
            BusinessDayConvention bdc,
            double vol,
            DayCounter dc,
            VolatilityType type = VolatilityType.ShiftedLognormal,
            double? shift = 0.0)
            : base(referenceDate, cal, bdc, dc)
        {
            volatility_ = new Handle<Quote>(new SimpleQuote(vol));
            maxSwapTenor_ = new Period(100, TimeUnit.Years);
            volatilityType_ = type;
            shift_ = shift;
        }

        // TermStructure interface
        public override Date maxDate() => Date.maxDate();

        public override double maxStrike() => double.MaxValue;

        // SwaptionVolatilityStructure interface
        public override Period maxSwapTenor() => maxSwapTenor_;

        // VolatilityTermStructure interface
        public override double minStrike() => double.MinValue;

        public override VolatilityType volatilityType() => volatilityType_;

        protected override double shiftImpl(double optionTime, double swapLength)
        {
            base.shiftImpl(optionTime, swapLength);
            return Convert.ToDouble(shift_);
        }

        protected new SmileSection smileSectionImpl(Date d, Period p)
        {
            var atmVol = volatility_.link.value();
            return new FlatSmileSection(d, atmVol, dayCounter(), referenceDate());
        }

        protected override SmileSection smileSectionImpl(double optionTime, double time)
        {
            var atmVol = volatility_.link.value();
            return new FlatSmileSection(optionTime, atmVol, dayCounter());
        }

        protected new double volatilityImpl(Date date, Period period, double rate) => volatility_.link.value();

        protected override double volatilityImpl(double time, double t, double rate) => volatility_.link.value();
    }
}
