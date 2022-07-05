/*
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)
               2017       Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using QLNet.Math.Optimization;

namespace QLNet.Math.Interpolations
{
    //! %SABR smile interpolation between discrete volatility points.
    [PublicAPI]
    public class SviInterpolation : Interpolation
    {
        private XABRCoeffHolder<SVISpecs> coeffs_;

        public SviInterpolation(List<double> xBegin, // x = strikes
            int size,
            List<double> yBegin, // y = volatilities
            double t, // option expiry
            double forward,
            double? a,
            double? b,
            double? sigma,
            double? rho,
            double? m,
            bool aIsFixed,
            bool bIsFixed,
            bool sigmaIsFixed,
            bool rhoIsFixed,
            bool mIsFixed, bool vegaWeighted = true,
            EndCriteria endCriteria = null,
            OptimizationMethod optMethod = null,
            double errorAccept = 0.0020,
            bool useMaxError = false,
            int maxGuesses = 50,
            List<double?> addParams = null)
        {
            impl_ = new XABRInterpolationImpl<SVISpecs>(
                xBegin, size, yBegin, t, forward,
                new List<double?> { a, b, sigma, rho, m },
                new List<bool> { aIsFixed, bIsFixed, sigmaIsFixed, rhoIsFixed, mIsFixed },
                vegaWeighted, endCriteria, optMethod, errorAccept, useMaxError,
                maxGuesses, addParams);
            coeffs_ = (impl_ as XABRInterpolationImpl<SVISpecs>).coeff_;
        }

        public double a() => coeffs_.params_[0].Value;

        public double b() => coeffs_.params_[1].Value;

        public EndCriteria.Type endCriteria() => coeffs_.XABREndCriteria_;

        public double expiry() => coeffs_.t_;

        public double forward() => coeffs_.forward_;

        public List<double> interpolationWeights() => coeffs_.weights_;

        public double m() => coeffs_.params_[4].Value;

        public double maxError() => coeffs_.maxError_.Value;

        public double rho() => coeffs_.params_[3].Value;

        public double rmsError() => coeffs_.error_.Value;

        public double sigma() => coeffs_.params_[2].Value;
    }

    //! %SABR interpolation factory and traits
}
