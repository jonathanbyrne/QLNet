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
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    //! %bilinear interpolation between discrete points
    [PublicAPI]
    public class BilinearInterpolation : Interpolation2D
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
}
