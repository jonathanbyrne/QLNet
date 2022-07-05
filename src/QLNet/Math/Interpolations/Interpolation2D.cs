/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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

namespace QLNet.Math.Interpolations
{
    //! base class for 2-D interpolations.
    /*! Classes derived from this class will provide interpolated
        values from two sequences of length \f$ N \f$ and \f$ M \f$,
        representing the discretized values of the \f$ x \f$ and \f$ y
        \f$ variables, and a \f$ N \times M \f$ matrix representing
        the tabulated function values.
    */

    // Interpolation factory

    public abstract class Interpolation2D : Extrapolator /*, IValue */
    {
        public abstract class templateImpl : Impl
        {
            protected List<double> xBegin_;
            protected int xSize_;
            protected List<double> yBegin_;
            protected int ySize_;
            protected Matrix zData_;

            // this method should be used for initialisation
            protected templateImpl(List<double> xBegin, int xSize,
                List<double> yBegin, int ySize,
                Matrix zData)
            {
                xBegin_ = xBegin;
                xSize_ = xSize;
                yBegin_ = yBegin;
                ySize_ = ySize;
                zData_ = zData;

                if (xSize < 2)
                {
                    throw new ArgumentException("not enough points to interpolate: at least 2 required, "
                                                + xSize + " provided");
                }

                if (ySize < 2)
                {
                    throw new ArgumentException("not enough points to interpolate: at least 2 required, "
                                                + ySize + " provided");
                }
            }

            public abstract void calculate();

            public abstract double value(double x, double y);

            public bool isInRange(double x, double y)
            {
                double x1 = xMin(), x2 = xMax();
                var xIsInrange = x >= x1 && x <= x2 || Utils.close(x, x1) || Utils.close(x, x2);
                if (!xIsInrange)
                {
                    return false;
                }

                double y1 = yMin(), y2 = yMax();
                return y >= y1 && y <= y2 || Utils.close(y, y1) || Utils.close(y, y2);
            }

            public int locateX(double x)
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
                result = System.Math.Max(System.Math.Min(result, xSize_ - 2), 0);
                return result;
            }

            public int locateY(double y)
            {
                var result = yBegin_.BinarySearch(y);
                if (result < 0)
                    // The upper_bound() algorithm finds the last position in a sequence that value can occupy
                    // without violating the sequence's ordering
                    // if BinarySearch does not find value the value, the index of the next larger item is returned
                {
                    result = ~result - 1;
                }

                // impose limits. we need the one before last at max or the first at min
                result = System.Math.Max(System.Math.Min(result, ySize_ - 2), 0);
                return result;
            }

            public double xMax() => xBegin_[xSize_ - 1];

            public double xMin() => xBegin_.First();

            public List<double> xValues() => xBegin_.GetRange(0, xSize_);

            public double yMax() => yBegin_[ySize_ - 1];

            public double yMin() => yBegin_.First();

            public List<double> yValues() => yBegin_.GetRange(0, ySize_);

            public Matrix zData() => zData_;
        }

        //! abstract base class for 2-D interpolation implementations
        protected interface Impl //: IValue
        {
            void calculate();

            bool isInRange(double x, double y);

            int locateX(double x);

            int locateY(double y);

            double value(double x, double y);

            double xMax();

            double xMin();

            List<double> xValues();

            double yMax();

            double yMin();

            List<double> yValues();

            Matrix zData();
        }

        protected Impl impl_;

        public bool isInRange(double x, double y) => impl_.isInRange(x, y);

        public int locateX(double x) => impl_.locateX(x);

        public int locateY(double y) => impl_.locateY(y);

        public override void update()
        {
            impl_.calculate();
        }

        // main method to derive an interpolated point
        public double value(double x, double y) => value(x, y, false);

        public double value(double x, double y, bool allowExtrapolation)
        {
            checkRange(x, y, allowExtrapolation);
            return impl_.value(x, y);
        }

        public double xMax() => impl_.xMax();

        public double xMin() => impl_.xMin();

        public List<double> xValues() => impl_.xValues();

        public double yMax() => impl_.yMax();

        public double yMin() => impl_.yMin();

        public List<double> yValues() => impl_.yValues();

        public Matrix zData() => impl_.zData();

        protected void checkRange(double x, double y, bool extrapolate)
        {
            if (!(extrapolate || allowsExtrapolation() || impl_.isInRange(x, y)))
            {
                throw new ArgumentException("interpolation range is [" + impl_.xMin() + ", " + impl_.xMax()
                                            + "] X [" + x + impl_.yMin() + ", " + impl_.yMax()
                                            + "]: extrapolation at (" + x + ", " + y + " not allowed");
            }
        }
    }
}
