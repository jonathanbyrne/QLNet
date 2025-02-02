﻿/*
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

using JetBrains.Annotations;
using QLNet.Math.Distributions;

namespace QLNet.Math
{
    /*! Kernel function in the statistical sense, e.g. a nonnegative,
        real-valued function which integrates to one and is symmetric.

        Derived classes will serve as functors.
    */

    //! Gaussian kernel function
    [PublicAPI]
    public class GaussianKernel : IKernelFunction
    {
        private CumulativeNormalDistribution cnd_;
        private NormalDistribution nd_;
        private double normFact_;

        public GaussianKernel(double average, double sigma)
        {
            nd_ = new NormalDistribution(average, sigma);
            cnd_ = new CumulativeNormalDistribution(average, sigma);
            // normFact is \sqrt{2*\pi}.
            normFact_ = Const.M_SQRT2 * Const.M_SQRTPI;
        }

        public double derivative(double x) => nd_.derivative(x) * normFact_;

        public double primitive(double x) => cnd_.value(x) * normFact_;

        public double value(double x) => nd_.value(x) * normFact_;
    }
}
