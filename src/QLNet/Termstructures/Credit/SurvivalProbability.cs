/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Termstructures.Credit
{
    /// <summary>
    ///     Survival-Probability-curve traits
    /// </summary>
    [PublicAPI]
    public class SurvivalProbability : ITraits<DefaultProbabilityTermStructure>
    {
        private const double avgHazardRate = 0.01;
        private const double maxHazardRate = 1.0;

        public double discountImpl(Interpolation i, double t) => i.value(t, true);

        public double forwardImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double guess(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData) // previous iteration value
            {
                return c.data()[i];
            }

            if (i == 1) // first pillar
            {
                return 1.0 / (1.0 + avgHazardRate * 0.25);
            }

            // extrapolate
            var d = c.dates()[i];
            return ((DefaultProbabilityTermStructure)c).survivalProbability(d, true);
        }

        public Date initialDate(DefaultProbabilityTermStructure c) => c.referenceDate(); // start of curve data

        public double initialValue(DefaultProbabilityTermStructure c) => 1; // value at reference date

        public int maxIterations() => 50; // upper bound for convergence loop

        public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f) =>
            // survival probability cannot increase
            c.data()[i - 1];

        public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData)
            {
                return c.data().Last() / 2.0;
            }

            var dt = c.times()[i] - c.times()[i - 1];
            return c.data()[i - 1] * System.Math.Exp(-maxHazardRate * dt);
        }

        public void updateGuess(List<double> data, double discount, int i)
        {
            data[i] = discount;
        }

        public double zeroYieldImpl(Interpolation i, double t) => throw new NotSupportedException();
    }
}
