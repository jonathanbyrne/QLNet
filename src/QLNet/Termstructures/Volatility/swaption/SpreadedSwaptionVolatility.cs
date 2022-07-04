//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using QLNet.Quotes;
using QLNet.Termstructures.Volatility;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.swaption
{
    [JetBrains.Annotations.PublicAPI] public class SpreadedSwaptionVolatility : SwaptionVolatilityStructure
    {
        public SpreadedSwaptionVolatility(Handle<SwaptionVolatilityStructure> baseVol, Handle<Quote> spread)
           : base(baseVol.link.businessDayConvention(), baseVol.link.dayCounter())
        {
            baseVol_ = baseVol;
            spread_ = spread;

            enableExtrapolation(baseVol.link.allowsExtrapolation());
            baseVol_.registerWith(update);
            spread_.registerWith(update);
        }
        // All virtual methods of base classes must be forwarded
        // TermStructure interface
        public override DayCounter dayCounter() => baseVol_.link.dayCounter();

        public override Date maxDate() => baseVol_.link.maxDate();

        public override double maxTime() => baseVol_.link.maxTime();

        public override Date referenceDate() => baseVol_.link.referenceDate();

        public override Calendar calendar() => baseVol_.link.calendar();

        public override int settlementDays() => baseVol_.link.settlementDays();

        // VolatilityTermStructure interface
        public override double minStrike() => baseVol_.link.minStrike();

        public override double maxStrike() => baseVol_.link.maxStrike();

        // SwaptionVolatilityStructure interface
        public override Period maxSwapTenor() => baseVol_.link.maxSwapTenor();

        public override VolatilityType volatilityType() => baseVol_.link.volatilityType();

        // SwaptionVolatilityStructure interface
        protected override SmileSection smileSectionImpl(Date optionDate, Period swapTenor)
        {
            var baseSmile = baseVol_.link.smileSection(optionDate, swapTenor, true);
            return new SpreadedSmileSection(baseSmile, spread_);
        }
        protected override SmileSection smileSectionImpl(double optionTime, double swapLength)
        {
            var baseSmile = baseVol_.link.smileSection(optionTime, swapLength, true);
            return new SpreadedSmileSection(baseSmile, spread_);
        }
        protected override double volatilityImpl(Date optionDate, Period swapTenor, double strike) => baseVol_.link.volatility(optionDate, swapTenor, strike, true) + spread_.link.value();

        protected override double volatilityImpl(double optionTime, double swapLength, double strike) => baseVol_.link.volatility(optionTime, swapLength, strike, true) + spread_.link.value();

        protected override double shiftImpl(double optionTime, double swapLength) => baseVol_.link.shift(optionTime, swapLength, true);

        private Handle<SwaptionVolatilityStructure> baseVol_;
        private Handle<Quote> spread_;

    }
}
