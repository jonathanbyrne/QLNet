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

using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility
{
    [JetBrains.Annotations.PublicAPI] public class AtmSmileSection : SmileSection
    {
        public AtmSmileSection(SmileSection source, double? atm = null)
        {
            source_ = source;
            f_ = atm;
            if (f_ == null)
                f_ = source_.atmLevel();
        }

        public override double minStrike() => source_.minStrike();

        public override double maxStrike() => source_.maxStrike();

        public override double? atmLevel() => f_;

        public override Date exerciseDate() => source_.exerciseDate();

        public override double exerciseTime() => source_.exerciseTime();

        public override DayCounter dayCounter() => source_.dayCounter();

        public override Date referenceDate() => source_.referenceDate();

        public override VolatilityType volatilityType() => source_.volatilityType();

        public override double shift() => source_.shift();

        protected override double volatilityImpl(double strike) => source_.volatility(strike);

        protected override double varianceImpl(double strike) => source_.variance(strike);

        private SmileSection source_;
        private double? f_;
    }
}
