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

using JetBrains.Annotations;
using QLNet.Quotes;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility
{
    [PublicAPI]
    public class SpreadedSmileSection : SmileSection
    {
        private Handle<Quote> spread_;
        private SmileSection underlyingSection_;

        public SpreadedSmileSection(SmileSection underlyingSection, Handle<Quote> spread)
        {
            underlyingSection_ = underlyingSection;
            spread_ = spread;

            underlyingSection_.registerWith(update);
            spread_.registerWith(update);
        }

        public override double? atmLevel() => underlyingSection_.atmLevel();

        public override DayCounter dayCounter() => underlyingSection_.dayCounter();

        public override Date exerciseDate() => underlyingSection_.exerciseDate();

        public override double exerciseTime() => underlyingSection_.exerciseTime();

        public override double maxStrike() => underlyingSection_.maxStrike();

        // SmileSection interface
        public override double minStrike() => underlyingSection_.minStrike();

        public override Date referenceDate() => underlyingSection_.referenceDate();

        public override double shift() => underlyingSection_.shift();

        // LazyObject interface
        public override void update()
        {
            notifyObservers();
        }

        public override VolatilityType volatilityType() => underlyingSection_.volatilityType();

        protected override double volatilityImpl(double k) => underlyingSection_.volatility(k) + spread_.link.value();
    }
}
