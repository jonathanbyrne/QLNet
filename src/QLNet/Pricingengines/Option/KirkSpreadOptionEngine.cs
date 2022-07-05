//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.Processes;
using QLNet.Quotes;

namespace QLNet.PricingEngines.Option
{
    /// <summary>
    ///     Kirk approximation for European spread option on futures
    /// </summary>
    [PublicAPI]
    public class KirkSpreadOptionEngine : SpreadOption.Engine
    {
        private BlackProcess process1_;
        private BlackProcess process2_;
        private Handle<Quote> rho_;

        public KirkSpreadOptionEngine(BlackProcess process1,
            BlackProcess process2,
            Handle<Quote> correlation)
        {
            process1_ = process1;
            process2_ = process2;
            rho_ = correlation;
        }

        public override void calculate()
        {
            // First: tests on types
            QLNet.Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () =>
                "not an European Option");

            var payoff = arguments_.payoff as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "not a plain-vanilla payoff");

            // forward values - futures, so b=0
            var forward1 = process1_.stateVariable().link.value();
            var forward2 = process2_.stateVariable().link.value();

            var exerciseDate = arguments_.exercise.lastDate();

            // Volatilities
            var sigma1 = process1_.blackVolatility().link.blackVol(exerciseDate,
                forward1);
            var sigma2 = process2_.blackVolatility().link.blackVol(exerciseDate,
                forward2);

            var riskFreeDiscount = process1_.riskFreeRate().link.discount(exerciseDate);

            var strike = payoff.strike();

            // Unique F (forward) value for pricing
            var F = forward1 / (forward2 + strike);

            // Its volatility
            var sigma =
                System.Math.Sqrt(System.Math.Pow(sigma1, 2)
                                 + System.Math.Pow(sigma2 * (forward2 / (forward2 + strike)), 2)
                                 - 2 * rho_.link.value() * sigma1 * sigma2 * (forward2 / (forward2 + strike)));

            // Day counter and Dates handling variables
            var rfdc = process1_.riskFreeRate().link.dayCounter();
            var t = rfdc.yearFraction(process1_.riskFreeRate().link.referenceDate(),
                arguments_.exercise.lastDate());

            // Black-Scholes solution values
            var d1 = (System.Math.Log(F) + 0.5 * System.Math.Pow(sigma,
                2) * t) / (sigma * System.Math.Sqrt(t));
            var d2 = d1 - sigma * System.Math.Sqrt(t);

            var pdf = new NormalDistribution();
            var cum = new CumulativeNormalDistribution();
            var Nd1 = cum.value(d1);
            var Nd2 = cum.value(d2);
            var NMd1 = cum.value(-d1);
            var NMd2 = cum.value(-d2);

            var optionType = payoff.optionType();

            if (optionType == QLNet.Option.Type.Call)
            {
                results_.value = riskFreeDiscount * (F * Nd1 - Nd2) * (forward2 + strike);
            }
            else
            {
                results_.value = riskFreeDiscount * (NMd2 - F * NMd1) * (forward2 + strike);
            }

            var callValue = optionType == QLNet.Option.Type.Call ? results_.value : riskFreeDiscount * (F * Nd1 - Nd2) * (forward2 + strike);
            results_.theta = System.Math.Log(riskFreeDiscount) / t * callValue +
                             riskFreeDiscount * (forward1 * sigma) / (2 * System.Math.Sqrt(t)) * pdf.value(d1);
        }
    }
}
