//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using QLNet.Math.Optimization;

namespace QLNet.Math.Interpolations
{
    //! %Abcd interpolation between discrete points.
    /*! \ingroup interpolations */
    [PublicAPI]
    public class AbcdInterpolation : Interpolation
    {
        private AbcdCoeffHolder coeffs_;

        /*! Constructor */
        public AbcdInterpolation(List<double> xBegin, int size, List<double> yBegin,
            double a = -0.06,
            double b = 0.17,
            double c = 0.54,
            double d = 0.17,
            bool aIsFixed = false,
            bool bIsFixed = false,
            bool cIsFixed = false,
            bool dIsFixed = false,
            bool vegaWeighted = false,
            EndCriteria endCriteria = null,
            OptimizationMethod optMethod = null)
        {
            impl_ = new AbcdInterpolationImpl(xBegin, size, yBegin,
                a, b, c, d,
                aIsFixed, bIsFixed,
                cIsFixed, dIsFixed,
                vegaWeighted,
                endCriteria,
                optMethod);
            impl_.update();
            coeffs_ = ((AbcdInterpolationImpl)impl_).AbcdCoeffHolder();
        }

        // Inspectors
        public double? a() => coeffs_.a_;

        public double? b() => coeffs_.b_;

        public double? c() => coeffs_.c_;

        public double? d() => coeffs_.d_;

        public EndCriteria.Type endCriteria() => coeffs_.abcdEndCriteria_;

        public List<double> k() => coeffs_.k_;

        public double k(double t, List<double> xBegin, int size)
        {
            var li = new LinearInterpolation(xBegin, size, coeffs_.k_);
            return li.value(t);
        }

        public double? maxError() => coeffs_.maxError_;

        public double? rmsError() => coeffs_.error_;
    }

    //! %Abcd interpolation factory and traits
    /*! \ingroup interpolations */
}
