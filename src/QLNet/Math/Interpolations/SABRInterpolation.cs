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
using QLNet.Math.Optimization;
using QLNet.Termstructures.Volatility;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Math.Interpolations
{
    //! %SABR smile interpolation between discrete volatility points.
    // For volatility ExerciseType Normal and when the forward < 0, it is suggested to fix beta = 0.0
    [PublicAPI]
    public class SABRInterpolation : Interpolation
    {
        private XABRCoeffHolder<SABRSpecs> coeffs_;

        public SABRInterpolation(List<double> xBegin, // x = strikes
            int xEnd,
            List<double> yBegin, // y = volatilities
            double t, // option expiry
            double forward,
            double? alpha,
            double? beta,
            double? nu,
            double? rho,
            bool alphaIsFixed,
            bool betaIsFixed,
            bool nuIsFixed,
            bool rhoIsFixed,
            bool vegaWeighted = true,
            EndCriteria endCriteria = null,
            OptimizationMethod optMethod = null,
            double errorAccept = 0.0020,
            bool useMaxError = false,
            int maxGuesses = 50,
            double shift = 0.0,
            VolatilityType volatilityType = VolatilityType.ShiftedLognormal,
            SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002)
        {
            var addParams = new List<double?>();
            addParams.Add(shift);
            addParams.Add(volatilityType == VolatilityType.ShiftedLognormal ? 0.0 : 1.0);
            addParams.Add((double?)approximationModel);

            impl_ = new XABRInterpolationImpl<SABRSpecs>(
                xBegin, xEnd, yBegin, t, forward,
                new List<double?> { alpha, beta, nu, rho },
                //boost::assign::list_of(alpha)(beta)(nu)(rho),
                new List<bool> { alphaIsFixed, betaIsFixed, nuIsFixed, rhoIsFixed },
                //boost::assign::list_of(alphaIsFixed)(betaIsFixed)(nuIsFixed)(rhoIsFixed),
                vegaWeighted, endCriteria, optMethod, errorAccept, useMaxError,
                maxGuesses, addParams);
            coeffs_ = (impl_ as XABRInterpolationImpl<SABRSpecs>).coeff_;
        }

        public double alpha() => coeffs_.params_[0].Value;

        public double beta() => coeffs_.params_[1].Value;

        public EndCriteria.Type endCriteria() => coeffs_.XABREndCriteria_;

        public double expiry() => coeffs_.t_;

        public double forward() => coeffs_.forward_;

        public List<double> interpolationWeights() => coeffs_.weights_;

        public double maxError() => coeffs_.maxError_.Value;

        public double nu() => coeffs_.params_[2].Value;

        public double rho() => coeffs_.params_[3].Value;

        public double rmsError() => coeffs_.error_.Value;
    }

    //! %SABR interpolation factory and traits
}
