/*
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class BicubicSplineImpl : Interpolation2D.templateImpl, IBicubicSplineDerivatives
    {
        private List<Interpolation> splines_;

        public BicubicSplineImpl(List<double> xBegin, int size, List<double> yBegin, int ySize, Matrix zData)
            : base(xBegin, size, yBegin, ySize, zData)
        {
            calculate();
        }

        public override void calculate()
        {
            splines_ = new List<Interpolation>(zData_.rows());
            for (var i = 0; i < zData_.rows(); ++i)
            {
                splines_.Add(new CubicInterpolation(xBegin_, xSize_, zData_.row(i),
                    CubicInterpolation.DerivativeApprox.Spline, false,
                    CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                    CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0));
            }
        }

        public double derivativeX(double x, double y)
        {
            List<double> section = new InitializedList<double>(zData_.columns());
            for (var i = 0; i < section.Count; ++i)
            {
                section[i] = value(xBegin_[i], y);
            }

            return new CubicInterpolation(xBegin_, xSize_, section,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0).derivative(x);
        }

        public double derivativeXY(double x, double y)
        {
            List<double> section = new InitializedList<double>(zData_.columns());
            for (var i = 0; i < section.Count; ++i)
            {
                section[i] = derivativeY(xBegin_[i], y);
            }

            return new CubicInterpolation(xBegin_, xSize_, section,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0).derivative(x);
        }

        public double derivativeY(double x, double y)
        {
            List<double> section = new InitializedList<double>(splines_.Count);
            for (var i = 0; i < splines_.Count; i++)
            {
                section[i] = splines_[i].value(x, true);
            }

            return new CubicInterpolation(yBegin_, ySize_, section,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0).derivative(y);
        }

        public double secondDerivativeX(double x, double y)
        {
            List<double> section = new InitializedList<double>(zData_.columns());
            for (var i = 0; i < section.Count; ++i)
            {
                section[i] = value(xBegin_[i], y);
            }

            return new CubicInterpolation(xBegin_, xSize_, section,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0).secondDerivative(x);
        }

        public double secondDerivativeY(double x, double y)
        {
            List<double> section = new InitializedList<double>(splines_.Count);
            for (var i = 0; i < splines_.Count; i++)
            {
                section[i] = splines_[i].value(x, true);
            }

            return new CubicInterpolation(yBegin_, ySize_, section,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0).secondDerivative(y);
        }

        public override double value(double x, double y)
        {
            List<double> section = new InitializedList<double>(splines_.Count);
            for (var i = 0; i < splines_.Count; i++)
            {
                section[i] = splines_[i].value(x, true);
            }

            var spline = new CubicInterpolation(yBegin_, ySize_, section,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0);
            return spline.value(y, true);
        }
    }

    //! bicubic-spline interpolation between discrete points
    /*! \todo revise end conditions */

    //! bicubic-spline-interpolation factory
}
