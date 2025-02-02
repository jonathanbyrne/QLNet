﻿/*
 Copyright (C) 2008-2014  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014  Edem Dawui (edawui@gmail.com)

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
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Termstructures.Yield;
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    [PublicAPI]
    public class ZeroInflationTraits : ITraits<ZeroInflationTermStructure>
    {
        private const double avgInflation = 0.02;
        private const double maxInflation = 0.5;

        public double discountImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double forwardImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double guess(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData) // previous iteration value
            {
                return c.data()[i];
            }

            if (i == 1) // first pillar
            {
                return avgInflation;
            }

            // could/should extrapolate
            return avgInflation;
        }

        public Date initialDate(ZeroInflationTermStructure t)
        {
            if (t.indexIsInterpolated())
            {
                return t.referenceDate() - t.observationLag();
            }

            return Utils.inflationPeriod(t.referenceDate() - t.observationLag(),
                t.frequency()).Key;
        }

        public double initialValue(ZeroInflationTermStructure t) => t.baseRate();

        public int maxIterations() => 5;

        public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData)
            {
                var r = c.data().Max();
                return r < 0.0 ? r / 2.0 : r * 2.0;
            }

            // no constraints.
            // We choose as max a value very unlikely to be exceeded.
            return maxInflation;
        }

        public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData)
            {
                var r = c.data().Min();
                return r < 0.0 ? r * 2.0 : r / 2.0;
            }

            return -maxInflation;
        }

        public void updateGuess(List<double> data, double discount, int i)
        {
            data[i] = discount;
        }

        public double zeroYieldImpl(Interpolation i, double t) => throw new NotSupportedException();
    }
}
