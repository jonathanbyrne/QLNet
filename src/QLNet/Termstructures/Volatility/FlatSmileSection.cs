/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility
{
    [PublicAPI]
    public class FlatSmileSection : SmileSection
    {
        private double? atmLevel_;
        private double vol_;

        public FlatSmileSection(Date d, double vol, DayCounter dc, Date referenceDate = null, double? atmLevel = null,
            VolatilityType type = VolatilityType.ShiftedLognormal, double shift = 0.0)
            : base(d, dc, referenceDate, type, shift)
        {
            vol_ = vol;
            atmLevel_ = atmLevel;
        }

        public FlatSmileSection(double exerciseTime, double vol, DayCounter dc, double? atmLevel = null,
            VolatilityType type = VolatilityType.ShiftedLognormal, double shift = 0.0)
            : base(exerciseTime, dc, type, shift)
        {
            vol_ = vol;
            atmLevel_ = atmLevel;
        }

        public override double? atmLevel() => atmLevel_;

        public override double maxStrike() => double.MaxValue;

        public override double minStrike() => double.MinValue - shift();

        protected override double volatilityImpl(double d) => vol_;
    }
}
