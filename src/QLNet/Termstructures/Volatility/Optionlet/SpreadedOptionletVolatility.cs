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
using QLNet.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet.Termstructures.Volatility.Optionlet
{
    [JetBrains.Annotations.PublicAPI] public class SpreadedOptionletVolatility : OptionletVolatilityStructure
    {
        public SpreadedOptionletVolatility(Handle<OptionletVolatilityStructure> baseVol, Handle<Quote> spread)
        {
            baseVol_ = baseVol;
            spread_ = spread;
            enableExtrapolation(baseVol.link.allowsExtrapolation());
            baseVol_.registerWith(update);
            spread_.registerWith(update);
        }
        // All virtual methods of base classes must be forwarded
        // VolatilityTermStructure interface
        public override BusinessDayConvention businessDayConvention() => baseVol_.link.businessDayConvention();

        public override double minStrike() => baseVol_.link.minStrike();

        public override double maxStrike() => baseVol_.link.maxStrike();

        // TermStructure interface
        public override DayCounter dayCounter() => baseVol_.link.dayCounter();

        public override Date maxDate() => baseVol_.link.maxDate();

        public override double maxTime() => baseVol_.link.maxTime();

        public override Date referenceDate() => baseVol_.link.referenceDate();

        public override Calendar calendar() => baseVol_.link.calendar();

        public override int settlementDays() => baseVol_.link.settlementDays();

        // All virtual methods of base classes must be forwarded
        // OptionletVolatilityStructure interface
        protected override SmileSection smileSectionImpl(Date d)
        {
            var baseSmile = baseVol_.link.smileSection(d, true);
            return new SpreadedSmileSection(baseSmile, spread_);
        }
        protected override SmileSection smileSectionImpl(double optionTime)
        {
            var baseSmile = baseVol_.link.smileSection(optionTime, true);
            return new SpreadedSmileSection(baseSmile, spread_);
        }
        protected override double volatilityImpl(double t, double s) => baseVol_.link.volatility(t, s, true) + spread_.link.value();

        private Handle<OptionletVolatilityStructure> baseVol_;
        private Handle<Quote> spread_;

    }
}
