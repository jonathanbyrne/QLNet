﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.legacy.libormarketmodels
{
    [PublicAPI]
    public class LmFixedVolatilityModel : LmVolatilityModel
    {
        private List<double> startTimes_;
        private Vector volatilities_;

        public LmFixedVolatilityModel(Vector volatilities, List<double> startTimes)
            : base(startTimes.Count, 0)
        {
            volatilities_ = volatilities;
            startTimes_ = startTimes;

            QLNet.Utils.QL_REQUIRE(startTimes_.Count > 1, () => "too few dates");

            QLNet.Utils.QL_REQUIRE(volatilities_.size() == startTimes_.Count, () =>
                "volatility array and fixing time array have to have the same size");

            for (var i = 1; i < startTimes_.Count; i++)
            {
                QLNet.Utils.QL_REQUIRE(startTimes_[i] > startTimes_[i - 1], () =>
                    "invalid time (" + startTimes_[i] + ", vs " + startTimes_[i - 1] + ")");
            }
        }

        public override void generateArguments()
        {
        }

        public override Vector volatility(double t, Vector x = null)
        {
            QLNet.Utils.QL_REQUIRE(t >= startTimes_.First() && t <= startTimes_.Last(), () =>
                "invalid time given for volatility model");

            var ti = startTimes_.GetRange(0, startTimes_.Count - 1).BinarySearch(t);
            if (ti < 0)
                // The upper_bound() algorithm finds the last position in a sequence that value can occupy
                // without violating the sequence's ordering
                // if BinarySearch does not find value the value, the index of the next larger item is returned
            {
                ti = ~ti - 1;
            }

            // impose limits. we need the one before last at max or the first at min
            ti = System.Math.Max(System.Math.Min(ti, startTimes_.Count - 2), 0);

            var tmp = new Vector(size_, 0.0);

            for (var i = ti; i < size_; ++i)
            {
                tmp[i] = volatilities_[i - ti];
            }

            return tmp;
        }

        public override double volatility(int i, double t, Vector x = null)
        {
            QLNet.Utils.QL_REQUIRE(t >= startTimes_.First() && t <= startTimes_.Last(), () =>
                "invalid time given for volatility model");

            var ti = startTimes_.GetRange(0, startTimes_.Count - 1).BinarySearch(t);
            if (ti < 0)
                // The upper_bound() algorithm finds the last position in a sequence that value can occupy
                // without violating the sequence's ordering
                // if BinarySearch does not find value the value, the index of the next larger item is returned
            {
                ti = ~ti - 1;
            }

            // impose limits. we need the one before last at max or the first at min
            ti = System.Math.Max(System.Math.Min(ti, startTimes_.Count - 2), 0);

            return volatilities_[i - ti];
        }
    }
}
