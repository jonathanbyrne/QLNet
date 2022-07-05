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
    /*
      Grid Explanation:

      Grid=[  (x1,y1) (x1,y2) (x1,y3)... (x1,yM)
              (x2,y1) (x2,y2) (x2,y3)... (x2,yM)
              .
              .
              .
              (xN,y1) (xN,y2) (xN,y3)... (xN,yM)
           ]

      The Passed variables are:
      - x which is N dimensional
      - y which is M dimensional
      - zData which is NxM dimensional and has the z values
        corresponding to the grid above.
      - kernel is a template which needs a Real operator()(Real x) implementation
    */

    /*! Implementation of the 2D kernel interpolation approach, which
          can be found in "Foreign Exchange Risk" by Hakala, Wystup page
          256.

          The kernel in the implementation is kept general, although a
          Gaussian is considered in the cited text.
    */
    [PublicAPI]
    public class KernelInterpolation2D : Interpolation2D
    {
        /*! \pre the \f$ x \f$ values must be sorted.
              \pre kernel needs a Real operator()(Real x) implementation

         */

        public KernelInterpolation2D(List<double> xBegin, int size, List<double> yBegin, int ySize,
            Matrix zData, IKernelFunction kernel)
        {
            impl_ = new KernelInterpolation2DImpl<IKernelFunction>(xBegin, size, yBegin, ySize, zData, kernel);
            update();
        }
    }
}
