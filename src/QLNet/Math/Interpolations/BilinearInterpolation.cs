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

using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class BilinearInterpolationImpl : Interpolation2D.templateImpl
    {
        public BilinearInterpolationImpl(List<double> xBegin, int xSize,
                                         List<double> yBegin, int ySize,
                                         Matrix zData)
           : base(xBegin, xSize, yBegin, ySize, zData)
        {
            calculate();
        }

        public override void calculate()
        { }

        public override double value(double x, double y)
        {
            int i = locateX(x), j = locateY(y);

            var z1 = zData_[j, i];
            var z2 = zData_[j, i + 1];
            var z3 = zData_[j + 1, i];
            var z4 = zData_[j + 1, i + 1];

            var t = (x - xBegin_[i]) /
                    (xBegin_[i + 1] - xBegin_[i]);
            var u = (y - yBegin_[j]) /
                    (yBegin_[j + 1] - yBegin_[j]);

            return (1.0 - t) * (1.0 - u) * z1 + t * (1.0 - u) * z2
                   + (1.0 - t) * u * z3 + t * u * z4;
        }
    }

    //! %bilinear interpolation between discrete points
    [JetBrains.Annotations.PublicAPI] public class BilinearInterpolation : Interpolation2D
    {
        /*! \pre the \f$ x \f$ and \f$ y \f$ values must be sorted. */

        public BilinearInterpolation(List<double> xBegin, int xSize,
                                     List<double> yBegin, int ySize,
                                     Matrix zData)
        {
            impl_ =
                       new BilinearInterpolationImpl(xBegin, xSize,
                                                     yBegin, ySize, zData);
        }
    }

    //! bilinear-interpolation factory
    [JetBrains.Annotations.PublicAPI] public class Bilinear : IInterpolationFactory2D
    {
        public Interpolation2D interpolate(List<double> xBegin, int xSize,
                                           List<double> yBegin, int ySize,
                                           Matrix zData) =>
            new BilinearInterpolation(xBegin, xSize, yBegin, ySize, zData);
    }
}
