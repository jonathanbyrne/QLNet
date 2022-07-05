/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using QLNet.Instruments;
using QLNet.Math.integrals;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    //! Pricing engine for European vanilla options using integral approach
    //    ! \todo define tolerance for calculate()
    //
    //        \ingroup vanillaengines
    //
    [PublicAPI]
    public class IntegralEngine : OneAssetOption.Engine
    {
        private GeneralizedBlackScholesProcess process_;

        public IntegralEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;

            process_.registerWith(update);
        }

        public override void calculate()
        {
            QLNet.Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () => "not an European Option");

            var payoff = arguments_.payoff as StrikedTypePayoff;

            QLNet.Utils.QL_REQUIRE(payoff != null, () => "not an European Option");

            var variance = process_.blackVolatility().link.blackVariance(arguments_.exercise.lastDate(), payoff.strike());

            var dividendDiscount = process_.dividendYield().link.discount(arguments_.exercise.lastDate());
            var riskFreeDiscount = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate());
            var drift = System.Math.Log(dividendDiscount / riskFreeDiscount) - 0.5 * variance;

            var f = new Integrand(arguments_.payoff, process_.stateVariable().link.value(), drift, variance);
            var integrator = new SegmentIntegral(5000);

            var infinity = 10.0 * System.Math.Sqrt(variance);
            results_.value = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate()) /
                             System.Math.Sqrt(2.0 * System.Math.PI * variance) *
                             integrator.value(f.value, drift - infinity, drift + infinity);
        }
    }
}
