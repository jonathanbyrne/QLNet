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
using System;
using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    //! Kernel interpolation between discrete points
    /*! Implementation of the kernel interpolation approach, which can
       be found in "Foreign Exchange Risk" by Hakala, Wystup page
       256.

       The kernel in the implementation is kept general, although a Gaussian
       is considered in the cited text.
    */
    [JetBrains.Annotations.PublicAPI] public class KernelInterpolation : Interpolation
    {

        /*! \pre the \f$ x \f$ values must be sorted.
           \pre kernel needs a Real operator()(Real x) implementation
        */
        public KernelInterpolation(List<double> xBegin, int size, List<double> yBegin, IKernelFunction kernel)
        {
            impl_ = new KernelInterpolationImpl<IKernelFunction>(xBegin, size, yBegin, kernel);
            impl_.update();
        }
    }


}
