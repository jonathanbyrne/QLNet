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
using QLNet.Instruments;
using QLNet.processes;
using System;
using QLNet.Pricingengines.vanilla;

namespace QLNet.Pricingengines.barrier
{
    //! Analytic pricing engine for American binary barriers options
    /*! The formulas are taken from "The complete guide to option pricing formulas 2nd Ed",
         E.G. Haug, McGraw-Hill, p.176 and following.

        \ingroup barrierengines

        \test
        - the correctness of the returned value in case of
          cash-or-nothing at-expiry binary payoff is tested by
          reproducing results available in literature.
        - the correctness of the returned value in case of
          asset-or-nothing at-expiry binary payoff is tested by
          reproducing results available in literature.
    */
    [JetBrains.Annotations.PublicAPI] public class AnalyticBinaryBarrierEngine : BarrierOption.Engine
    {
        public AnalyticBinaryBarrierEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }
        public override void calculate()
        {
            var ex = arguments_.exercise as AmericanExercise;
            Utils.QL_REQUIRE(ex != null, () => "non-American exercise given");
            Utils.QL_REQUIRE(ex.payoffAtExpiry(), () => "payoff must be at expiry");
            Utils.QL_REQUIRE(ex.dates()[0] <= process_.blackVolatility().link.referenceDate(), () =>
                             "American option with window exercise not handled yet");

            var payoff = arguments_.payoff as StrikedTypePayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-striked payoff given");

            var spot = process_.stateVariable().link.value();
            Utils.QL_REQUIRE(spot > 0.0, () => "negative or null underlying given");

            var variance = process_.blackVolatility().link.blackVariance(ex.lastDate(), payoff.strike());
            var barrier = arguments_.barrier;
            Utils.QL_REQUIRE(barrier > 0.0, () => "positive barrier value required");
            var barrierType = arguments_.barrierType;

            // KO degenerate cases
            if (barrierType == Barrier.Type.DownOut && spot <= barrier ||
                barrierType == Barrier.Type.UpOut && spot >= barrier)
            {
                // knocked out, no value
                results_.value = 0;
                results_.delta = 0;
                results_.gamma = 0;
                results_.vega = 0;
                results_.theta = 0;
                results_.rho = 0;
                results_.dividendRho = 0;
                return;
            }

            // KI degenerate cases
            if (barrierType == Barrier.Type.DownIn && spot <= barrier ||
                barrierType == Barrier.Type.UpIn && spot >= barrier)
            {
                // knocked in - is a digital european
                Exercise exercise = new EuropeanExercise(arguments_.exercise.lastDate());

                IPricingEngine engine = new AnalyticEuropeanEngine(process_);

                var opt = new VanillaOption(payoff, exercise);
                opt.setPricingEngine(engine);
                results_.value = opt.NPV();
                results_.delta = opt.delta();
                results_.gamma = opt.gamma();
                results_.vega = opt.vega();
                results_.theta = opt.theta();
                results_.rho = opt.rho();
                results_.dividendRho = opt.dividendRho();
                return;
            }

            var riskFreeDiscount = process_.riskFreeRate().link.discount(ex.lastDate());

            var helper = new AnalyticBinaryBarrierEngine_helper(
               process_, payoff, ex, arguments_);
            results_.value = helper.payoffAtExpiry(spot, variance, riskFreeDiscount);

        }

        private GeneralizedBlackScholesProcess process_;
    }

    // calc helper object
}
