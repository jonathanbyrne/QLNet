/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    //! Analytic pricing engine for European options with discrete dividends
    /*! \ingroup vanillaengines

        \test the correctness of the returned greeks is tested by
              reproducing numerical derivatives.
    */
    [PublicAPI]
    public class AnalyticDividendEuropeanEngine : DividendVanillaOption.Engine
    {
        private GeneralizedBlackScholesProcess process_;

        public AnalyticDividendEuropeanEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            QLNet.Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () => "not an European option");

            var payoff = arguments_.payoff as StrikedTypePayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-striked payoff given");

            var settlementDate = process_.riskFreeRate().link.referenceDate();
            var riskless = 0.0;
            int i;
            for (i = 0; i < arguments_.cashFlow.Count; i++)
            {
                if (arguments_.cashFlow[i].date() >= settlementDate)
                {
                    riskless += arguments_.cashFlow[i].amount() *
                                process_.riskFreeRate().link.discount(arguments_.cashFlow[i].date());
                }
            }

            var spot = process_.stateVariable().link.value() - riskless;
            QLNet.Utils.QL_REQUIRE(spot > 0.0, () => "negative or null underlying after subtracting dividends");

            var dividendDiscount = process_.dividendYield().link.discount(arguments_.exercise.lastDate());
            var riskFreeDiscount = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate());
            var forwardPrice = spot * dividendDiscount / riskFreeDiscount;

            var variance = process_.blackVolatility().link.blackVariance(arguments_.exercise.lastDate(),
                payoff.strike());

            var black = new BlackCalculator(payoff, forwardPrice, System.Math.Sqrt(variance), riskFreeDiscount);

            results_.value = black.value();
            results_.delta = black.delta(spot);
            results_.gamma = black.gamma(spot);

            var rfdc = process_.riskFreeRate().link.dayCounter();
            var voldc = process_.blackVolatility().link.dayCounter();
            var t = voldc.yearFraction(process_.blackVolatility().link.referenceDate(), arguments_.exercise.lastDate());
            results_.vega = black.vega(t);

            double delta_theta = 0.0, delta_rho = 0.0;
            for (i = 0; i < arguments_.cashFlow.Count; i++)
            {
                var d = arguments_.cashFlow[i].date();
                if (d >= settlementDate)
                {
                    delta_theta -= arguments_.cashFlow[i].amount() *
                                   process_.riskFreeRate().link.zeroRate(d, rfdc, Compounding.Continuous).value() *
                                   process_.riskFreeRate().link.discount(d);
                    var t1 = process_.time(d);
                    delta_rho += arguments_.cashFlow[i].amount() * t1 *
                                 process_.riskFreeRate().link.discount(t1);
                }
            }

            t = process_.time(arguments_.exercise.lastDate());
            try
            {
                results_.theta = black.theta(spot, t) +
                                 delta_theta * black.delta(spot);
            }
            catch (Exception)
            {
                results_.theta = null;
            }

            results_.rho = black.rho(t) + delta_rho * black.delta(spot);
        }
    }
}
