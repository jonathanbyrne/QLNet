﻿using System;
using System.Collections.Generic;
using System.Linq;
using QLNet.Math;
using QLNet.Termstructures.Yield;
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    [JetBrains.Annotations.PublicAPI] public class YoYInflationTraits : ITraits<YoYInflationTermStructure>
    {
        const double avgInflation = 0.02;
        const double maxInflation = 0.5;

        public Date initialDate(YoYInflationTermStructure t)
        {
            if (t.indexIsInterpolated())
            {
                return t.referenceDate() - t.observationLag();
            }
            else
            {
                return Utils.inflationPeriod(t.referenceDate() - t.observationLag(),
                    t.frequency()).Key;
            }
        }

        public double initialValue(YoYInflationTermStructure t) => t.baseRate();

        public double guess(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData)   // previous iteration value
                return c.data()[i];

            if (i == 1)   // first pillar
                return avgInflation;

            // could/should extrapolate
            return avgInflation;
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

        public void updateGuess(List<double> data, double discount, int i)
        {
            data[i] = discount;
        }

        public int maxIterations() => 40;

        public double discountImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double zeroYieldImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double forwardImpl(Interpolation i, double t) => throw new NotSupportedException();
    }
}