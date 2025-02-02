﻿/*
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
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    //! Pricing engine for European vanilla options using analytical formulae
    /*! \ingroup vanillaengines

        \test
        - the correctness of the returned value is tested by
          reproducing results available in literature.
        - the correctness of the returned greeks is tested by
          reproducing results available in literature.
        - the correctness of the returned greeks is tested by
          reproducing numerical derivatives.
        - the correctness of the returned implied volatility is tested
          by using it for reproducing the target value.
        - the implied-volatility calculation is tested by checking
          that it does not modify the option.
        - the correctness of the returned value in case of
          cash-or-nothing digital payoff is tested by reproducing
          results available in literature.
        - the correctness of the returned value in case of
          asset-or-nothing digital payoff is tested by reproducing
          results available in literature.
        - the correctness of the returned value in case of gap digital
          payoff is tested by reproducing results available in
          literature.
        - the correctness of the returned greeks in case of
          cash-or-nothing digital payoff is tested by reproducing
          numerical derivatives.
    */
    [PublicAPI]
    public class AnalyticEuropeanEngine : OneAssetOption.Engine
    {
        private GeneralizedBlackScholesProcess process_;

        public AnalyticEuropeanEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;

            process_.registerWith(update);
        }

        public override void calculate()
        {
            QLNet.Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () => "not an European option");

            var payoff = arguments_.payoff as StrikedTypePayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-striked payoff given");

            var variance = process_.blackVolatility().link.blackVariance(arguments_.exercise.lastDate(), payoff.strike());
            var dividendDiscount = process_.dividendYield().link.discount(arguments_.exercise.lastDate());
            var riskFreeDiscount = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate());
            var spot = process_.stateVariable().link.value();
            QLNet.Utils.QL_REQUIRE(spot > 0.0, () => "negative or null underlying given");
            var forwardPrice = spot * dividendDiscount / riskFreeDiscount;

            var black = new BlackCalculator(payoff, forwardPrice, System.Math.Sqrt(variance), riskFreeDiscount);

            results_.value = black.value();
            results_.delta = black.delta(spot);
            results_.deltaForward = black.deltaForward();
            results_.elasticity = black.elasticity(spot);
            results_.gamma = black.gamma(spot);

            var rfdc = process_.riskFreeRate().link.dayCounter();
            var divdc = process_.dividendYield().link.dayCounter();
            var voldc = process_.blackVolatility().link.dayCounter();
            var t = rfdc.yearFraction(process_.riskFreeRate().link.referenceDate(), arguments_.exercise.lastDate());
            results_.rho = black.rho(t);

            t = divdc.yearFraction(process_.dividendYield().link.referenceDate(), arguments_.exercise.lastDate());
            results_.dividendRho = black.dividendRho(t);

            t = voldc.yearFraction(process_.blackVolatility().link.referenceDate(), arguments_.exercise.lastDate());
            results_.vega = black.vega(t);
            try
            {
                results_.theta = black.theta(spot, t);
                results_.thetaPerDay = black.thetaPerDay(spot, t);
            }
            catch
            {
                results_.theta = null;
                results_.thetaPerDay = null;
            }

            results_.strikeSensitivity = black.strikeSensitivity();
            results_.itmCashProbability = black.itmCashProbability();
        }
    }
}
