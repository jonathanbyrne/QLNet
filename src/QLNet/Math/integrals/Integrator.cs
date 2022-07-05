/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using QLNet.Extensions;

namespace QLNet.Math.integrals
{
    public abstract class Integrator
    {
        protected double? absoluteAccuracy_;
        protected double absoluteError_;
        protected int evaluations_;
        protected int maxEvaluations_;

        protected Integrator(double? absoluteAccuracy, int maxEvaluations)
        {
            absoluteAccuracy_ = absoluteAccuracy;
            maxEvaluations_ = maxEvaluations;
            if (absoluteAccuracy != null)
            {
                Utils.QL_REQUIRE(absoluteAccuracy > double.Epsilon, () =>
                    "required tolerance (" + absoluteAccuracy + ") not allowed. It must be > " + double.Epsilon);
            }
        }

        // Inspectors
        public double? absoluteAccuracy() => absoluteAccuracy_;

        public double absoluteError() => absoluteError_;

        public bool integrationSuccess() => evaluations_ <= maxEvaluations_ && absoluteError_ <= absoluteAccuracy_;

        public int maxEvaluations() => maxEvaluations_;

        public int numberOfEvaluations() => evaluations_;

        // Modifiers
        public void setAbsoluteAccuracy(double accuracy)
        {
            absoluteAccuracy_ = accuracy;
        }

        public void setMaxEvaluations(int maxEvaluations)
        {
            maxEvaluations_ = maxEvaluations;
        }

        public double value(Func<double, double> f, double a, double b)
        {
            evaluations_ = 0;
            if (a.IsEqual(b))
            {
                return 0.0;
            }

            if (b > a)
            {
                return integrate(f, a, b);
            }

            return -integrate(f, b, a);
        }

        protected abstract double integrate(Func<double, double> f, double a, double b);

        protected void increaseNumberOfEvaluations(int increase)
        {
            evaluations_ += increase;
        }

        protected void setAbsoluteError(double error)
        {
            absoluteError_ = error;
        }

        protected void setNumberOfEvaluations(int evaluations)
        {
            evaluations_ = evaluations;
        }
    }
}
