﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class FlatExtrapolator2D : Interpolation2D
    {
        protected class FlatExtrapolator2DImpl : Impl
        {
            private readonly Interpolation2D decoratedInterp_;

            public FlatExtrapolator2DImpl(Interpolation2D decoratedInterpolation)
            {
                decoratedInterp_ = decoratedInterpolation;
                calculate();
            }

            public void calculate()
            {
                // Nothing to do here
            }

            public bool isInRange(double x, double y) => decoratedInterp_.isInRange(x, y);

            public int locateX(double x) => decoratedInterp_.locateX(x);

            public int locateY(double y) => decoratedInterp_.locateY(y);

            public void update()
            {
                decoratedInterp_.update();
            }

            public double value(double x, double y)
            {
                x = bindX(x);
                y = bindY(y);
                return decoratedInterp_.value(x, y);
            }

            public double xMax() => decoratedInterp_.xMax();

            public double xMin() => decoratedInterp_.xMin();

            public List<double> xValues() => decoratedInterp_.xValues();

            public double yMax() => decoratedInterp_.yMax();

            public double yMin() => decoratedInterp_.yMin();

            public List<double> yValues() => decoratedInterp_.yValues();

            public Matrix zData() => decoratedInterp_.zData();

            private double bindX(double x)
            {
                if (x < xMin())
                {
                    return xMin();
                }

                if (x > xMax())
                {
                    return xMax();
                }

                return x;
            }

            private double bindY(double y)
            {
                if (y < yMin())
                {
                    return yMin();
                }

                if (y > yMax())
                {
                    return yMax();
                }

                return y;
            }
        }

        public FlatExtrapolator2D(Interpolation2D decoratedInterpolation)
        {
            impl_ = new FlatExtrapolator2DImpl(decoratedInterpolation);
        }
    }
}
