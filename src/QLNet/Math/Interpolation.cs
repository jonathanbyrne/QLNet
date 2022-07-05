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

using System;
using System.Collections.Generic;
using System.Linq;
using QLNet.Math.Interpolations;

namespace QLNet.Math
{
    //! base class for 1-D interpolations.
    /* Classes derived from this class will provide interpolated values from two sequences of equal length,
     * representing discretized values of a variable and a function of the former, respectively. */

    // Interpolation factory

    public abstract class Interpolation : Extrapolator, IValue
    {
        public abstract class templateImpl : Impl
        {
            protected int size_;
            protected List<double> xBegin_;
            protected List<double> yBegin_;

            // this method should be used for initialisation
            protected templateImpl(List<double> xBegin, int size, List<double> yBegin, int requiredPoints = 2)
            {
                xBegin_ = xBegin;
                yBegin_ = yBegin;
                size_ = size;
                if (size < requiredPoints)
                {
                    throw new ArgumentException("not enough points to interpolate: at least 2 required, "
                                                + size + " provided");
                }
            }

            public abstract double derivative(double d);

            public abstract double primitive(double d);

            public abstract double secondDerivative(double d);

            public abstract void update();

            public abstract double value(double d);

            public bool isInRange(double x)
            {
                double x1 = xMin(), x2 = xMax();
                return x >= x1 && x <= x2 || Math.Utils.close(x, x1) || Math.Utils.close(x, x2);
            }

            public double xMax() => xBegin_[size_ - 1];

            public double xMin() => xBegin_.First();

            public List<double> xValues() => xBegin_.GetRange(0, size_);

            public List<double> yValues() => yBegin_.GetRange(0, size_);

            protected int locate(double x)
            {
                var result = xBegin_.BinarySearch(x);
                if (result < 0)
                    // The upper_bound() algorithm finds the last position in a sequence that value can occupy
                    // without violating the sequence's ordering
                    // if BinarySearch does not find value the value, the index of the next larger item is returned
                {
                    result = ~result - 1;
                }

                // impose limits. we need the one before last at max or the first at min
                result = System.Math.Max(System.Math.Min(result, size_ - 2), 0);
                return result;
            }
        }

        // abstract base class interface for interpolation implementations
        protected interface Impl : IValue
        {
            double derivative(double d);

            bool isInRange(double d);

            double primitive(double d);

            double secondDerivative(double d);

            void update();

            double xMax();

            double xMin();

            List<double> xValues();

            List<double> yValues();
        }

        protected Impl impl_;

        public double derivative(double x, bool allowExtrapolation = false)
        {
            checkRange(x, allowExtrapolation);
            return impl_.derivative(x);
        }

        public bool empty() => impl_ == null;

        public double primitive(double x, bool allowExtrapolation = false)
        {
            checkRange(x, allowExtrapolation);
            return impl_.primitive(x);
        }

        public double secondDerivative(double x, bool allowExtrapolation = false)
        {
            checkRange(x, allowExtrapolation);
            return impl_.secondDerivative(x);
        }

        public override void update()
        {
            impl_.update();
        }

        // main method to derive an interpolated point
        public double value(double x) => value(x, false);

        public double value(double x, bool allowExtrapolation)
        {
            checkRange(x, allowExtrapolation);
            return impl_.value(x);
        }

        public double xMax() => impl_.xMax();

        public double xMin() => impl_.xMin();

        protected void checkRange(double x, bool extrap)
        {
            if (!(extrap || allowsExtrapolation() || isInRange(x)))
            {
                throw new ArgumentException("interpolation range is [" + impl_.xMin() + ", " + impl_.xMax()
                                            + "]: extrapolation at " + x + " not allowed");
            }
        }

        private bool isInRange(double x) => impl_.isInRange(x);
    }
}
