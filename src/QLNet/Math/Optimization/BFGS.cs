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

using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    //! Broyden-Fletcher-Goldfarb-Shanno algorithm
    /*! See <http://en.wikipedia.org/wiki/BFGS_method>.

        Adapted from Numerical Recipes in C, 2nd edition.

        User has to provide line-search method and optimization end criteria.
    */
    [PublicAPI]
    public class BFGS : LineSearchBasedMethod
    {
        // inverse of hessian matrix
        private Matrix inverseHessian_;

        public BFGS(LineSearch lineSearch = null)
            : base(lineSearch)
        {
            inverseHessian_ = new Matrix();
        }

        // LineSearchBasedMethod interface
        protected override Vector getUpdatedDirection(Problem P, double gold2, Vector oldGradient)
        {
            if (inverseHessian_.rows() == 0)
            {
                // first time in this update, we create needed structures
                inverseHessian_ = new Matrix(P.currentValue().size(), P.currentValue().size(), 0.0);
                for (var i = 0; i < P.currentValue().size(); ++i)
                {
                    inverseHessian_[i, i] = 1.0;
                }
            }

            var diffGradient = new Vector();
            var diffGradientWithHessianApplied = new Vector(P.currentValue().size(), 0.0);

            diffGradient = lineSearch_.lastGradient() - oldGradient;
            for (var i = 0; i < P.currentValue().size(); ++i)
            for (var j = 0; j < P.currentValue().size(); ++j)
            {
                diffGradientWithHessianApplied[i] += inverseHessian_[i, j] * diffGradient[j];
            }

            double fac, fae, fad;
            double sumdg, sumxi;

            fac = fae = sumdg = sumxi = 0.0;
            for (var i = 0; i < P.currentValue().size(); ++i)
            {
                fac += diffGradient[i] * lineSearch_.searchDirection[i];
                fae += diffGradient[i] * diffGradientWithHessianApplied[i];
                sumdg += System.Math.Pow(diffGradient[i], 2.0);
                sumxi += System.Math.Pow(lineSearch_.searchDirection[i], 2.0);
            }

            if (fac > System.Math.Sqrt(1e-8 * sumdg * sumxi)) // skip update if fac not sufficiently positive
            {
                fac = 1.0 / fac;
                fad = 1.0 / fae;

                for (var i = 0; i < P.currentValue().size(); ++i)
                {
                    diffGradient[i] = fac * lineSearch_.searchDirection[i] - fad * diffGradientWithHessianApplied[i];
                }

                for (var i = 0; i < P.currentValue().size(); ++i)
                for (var j = 0; j < P.currentValue().size(); ++j)
                {
                    inverseHessian_[i, j] += fac * lineSearch_.searchDirection[i] * lineSearch_.searchDirection[j];
                    inverseHessian_[i, j] -= fad * diffGradientWithHessianApplied[i] * diffGradientWithHessianApplied[j];
                    inverseHessian_[i, j] += fae * diffGradient[i] * diffGradient[j];
                }
            }

            var direction = new Vector(P.currentValue().size());
            for (var i = 0; i < P.currentValue().size(); ++i)
            {
                direction[i] = 0.0;
                for (var j = 0; j < P.currentValue().size(); ++j)
                {
                    direction[i] -= inverseHessian_[i, j] * lineSearch_.lastGradient()[j];
                }
            }

            return direction;
        }
    }
}
