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
using QLNet.Math;
using QLNet.Math.integrals;
using System;

namespace QLNet.legacy.libormarketmodels
{
    [JetBrains.Annotations.PublicAPI] public class LfmCovarianceProxy : LfmCovarianceParameterization
    {
        public LmVolatilityModel volaModel { get; set; }
        public LmCorrelationModel corrModel { get; set; }
        public LmVolatilityModel volaModel_ { get; set; }
        public LmCorrelationModel corrModel_ { get; set; }

        public LfmCovarianceProxy(LmVolatilityModel volaModel,
                                  LmCorrelationModel corrModel)
           : base(corrModel.size(), corrModel.factors())
        {
            volaModel_ = volaModel;
            corrModel_ = corrModel;

            Utils.QL_REQUIRE(volaModel_.size() == corrModel_.size(), () =>
                             "different size for the volatility (" + volaModel_.size() + ") and correlation (" + corrModel_.size() + ") models");
        }

        public LmVolatilityModel volatilityModel() => volaModel_;

        public LmCorrelationModel correlationModel() => corrModel_;

        public override Matrix diffusion(double t) => diffusion(t, null);

        public override Matrix diffusion(double t, Vector x)
        {
            var pca = corrModel_.pseudoSqrt(t, x);
            var vol = volaModel_.volatility(t, x);
            for (var i = 0; i < size_; ++i)
            {
                for (var j = 0; j < size_; ++j)
                    pca[i, j] = pca[i, j] * vol[i];
            }
            return pca;
        }

        public override Matrix covariance(double t, Vector x)
        {
            var volatility = volaModel_.volatility(t, x);
            var correlation = corrModel_.correlation(t, x);

            var tmp = new Matrix(size_, size_);
            for (var i = 0; i < size_; ++i)
            {
                for (var j = 0; j < size_; ++j)
                {
                    tmp[i, j] = volatility[i] * correlation[i, j] * volatility[j];
                }
            }
            return tmp;
        }

        public double integratedCovariance(int i, int j, double t) => integratedCovariance(i, j, t, new Vector());

        public double integratedCovariance(int i, int j, double t, Vector x)
        {
            if (corrModel_.isTimeIndependent())
            {
                try
                {
                    // if all objects support these methods
                    // thats by far the fastest way to get the
                    // integrated covariance
                    return corrModel_.correlation(i, j, 0.0, x)
                           * volaModel_.integratedVariance(j, i, t, x);
                    //verifier la methode integratedVariance, qui bdoit etre implémenté
                }
                catch (Exception)
                {
                    // okay proceed with the
                    // slow numerical integration routine
                }
            }

            try
            {
                Utils.QL_REQUIRE(x.empty() != false, () => "can not handle given x here");
            }
            catch   //OK x empty
            {
            }

            var tmp = 0.0;
            var helper = new VarProxy_Helper(this, i, j);

            var integrator = new GaussKronrodAdaptive(1e-10, 10000);
            for (var k = 0; k < 64; ++k)
            {
                tmp += integrator.value(helper.value, k * t / 64.0, (k + 1) * t / 64.0);
            }
            return tmp;
        }
    }

    [JetBrains.Annotations.PublicAPI] public class VarProxy_Helper
    {
        private int i_, j_;
        public LmVolatilityModel volaModel_ { get; set; }
        public LmCorrelationModel corrModel_ { get; set; }

        public VarProxy_Helper(LfmCovarianceProxy proxy, int i, int j)
        {
            i_ = i;
            j_ = j;
            volaModel_ = proxy.volaModel_;
            corrModel_ = proxy.corrModel_;
        }

        public double value(double t)
        {
            double v1, v2;
            if (i_ == j_)
            {
                v1 = v2 = volaModel_.volatility(i_, t, null);
            }
            else
            {
                v1 = volaModel_.volatility(i_, t, null);
                v2 = volaModel_.volatility(j_, t, null);
            }
            return v1 * corrModel_.correlation(i_, j_, t, null) * v2;
        }
    }
}
