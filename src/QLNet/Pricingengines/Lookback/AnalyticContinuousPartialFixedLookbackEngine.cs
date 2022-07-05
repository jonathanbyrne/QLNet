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

using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.Processes;

namespace QLNet.PricingEngines.Lookback
{
    //! Pricing engine for European continuous partial-time fixed-strike lookback options
    /*! Formula from "Option Pricing Formulas, Second Edition",
        E.G. Haug, 2006, p.148
    */
    [PublicAPI]
    public class AnalyticContinuousPartialFixedLookbackEngine : ContinuousPartialFixedLookbackOption.Engine
    {
        private CumulativeNormalDistribution f_ = new CumulativeNormalDistribution();
        private GeneralizedBlackScholesProcess process_;

        public AnalyticContinuousPartialFixedLookbackEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "Non-plain payoff given");

            QLNet.Utils.QL_REQUIRE(process_.x0() > 0.0, () => "negative or null underlying");

            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    QLNet.Utils.QL_REQUIRE(payoff.strike() >= 0.0, () => "Strike must be positive or null");
                    results_.value = A(1);
                    break;
                case QLNet.Option.Type.Put:
                    QLNet.Utils.QL_REQUIRE(payoff.strike() > 0.0, () => "Strike must be positive");
                    results_.value = A(-1);
                    break;
                default:
                    QLNet.Utils.QL_FAIL("Unknown ExerciseType");
                    break;
            }
        }

        private double A(double eta)
        {
            var differentStartOfLookback = lookbackPeriodStartTime().IsNotEqual(residualTime());
            var carry = riskFreeRate() - dividendYield();

            var vol = volatility();
            var x = 2.0 * carry / (vol * vol);
            var s = underlying() / strike();
            var ls = System.Math.Log(s);
            var d1 = ls / stdDeviation() + 0.5 * (x + 1.0) * stdDeviation();
            var d2 = d1 - stdDeviation();

            double e1 = 0, e2 = 0;
            if (differentStartOfLookback)
            {
                e1 = (carry + vol * vol / 2) * (residualTime() - lookbackPeriodStartTime()) / (vol * System.Math.Sqrt(residualTime() - lookbackPeriodStartTime()));
                e2 = e1 - vol * System.Math.Sqrt(residualTime() - lookbackPeriodStartTime());
            }

            var f1 = (ls + (carry + vol * vol / 2) * lookbackPeriodStartTime()) / (vol * System.Math.Sqrt(lookbackPeriodStartTime()));
            var f2 = f1 - vol * System.Math.Sqrt(lookbackPeriodStartTime());

            var n1 = f_.value(eta * d1);
            var n2 = f_.value(eta * d2);

            BivariateCumulativeNormalDistributionWe04DP cnbn1 = new BivariateCumulativeNormalDistributionWe04DP(-1),
                cnbn2 = new BivariateCumulativeNormalDistributionWe04DP(0),
                cnbn3 = new BivariateCumulativeNormalDistributionWe04DP(0);
            if (differentStartOfLookback)
            {
                cnbn1 = new BivariateCumulativeNormalDistributionWe04DP(-System.Math.Sqrt(lookbackPeriodStartTime() / residualTime()));
                cnbn2 = new BivariateCumulativeNormalDistributionWe04DP(System.Math.Sqrt(1 - lookbackPeriodStartTime() / residualTime()));
                cnbn3 = new BivariateCumulativeNormalDistributionWe04DP(-System.Math.Sqrt(1 - lookbackPeriodStartTime() / residualTime()));
            }

            var n3 = cnbn1.value(eta * (d1 - x * stdDeviation()), eta * (-f1 + 2.0 * carry * System.Math.Sqrt(lookbackPeriodStartTime()) / vol));
            var n4 = cnbn2.value(eta * e1, eta * d1);
            var n5 = cnbn3.value(-eta * e1, eta * d1);
            var n6 = cnbn1.value(eta * f2, -eta * d2);
            var n7 = f_.value(eta * f1);
            var n8 = f_.value(-eta * e2);

            var pow_s = System.Math.Pow(s, -x);
            var carryDiscount = System.Math.Exp(-carry * (residualTime() - lookbackPeriodStartTime()));
            return eta * (underlying() * dividendDiscount() * n1
                          - strike() * riskFreeDiscount() * n2
                          + underlying() * riskFreeDiscount() / x
                          * (-pow_s * n3 + dividendDiscount() / riskFreeDiscount() * n4)
                          - underlying() * dividendDiscount() * n5
                          - strike() * riskFreeDiscount() * n6
                          + carryDiscount * dividendDiscount()
                                          * (1 - 0.5 * vol * vol / carry) *
                                          underlying() * n7 * n8);
        }

        private double dividendDiscount() => process_.dividendYield().link.discount(residualTime());

        private double dividendYield() =>
            process_.dividendYield().link.zeroRate(residualTime(),
                Compounding.Continuous, Frequency.NoFrequency).value();

        private double lookbackPeriodStartTime() => process_.time(arguments_.lookbackPeriodStart);

        private double residualTime() => process_.time(arguments_.exercise.lastDate());

        private double riskFreeDiscount() => process_.riskFreeRate().link.discount(residualTime());

        private double riskFreeRate() =>
            process_.riskFreeRate().link.zeroRate(residualTime(),
                Compounding.Continuous, Frequency.NoFrequency).value();

        private double stdDeviation() => volatility() * System.Math.Sqrt(residualTime());

        private double strike()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "Non-plain payoff given");
            return payoff.strike();
        }

        // helper methods
        private double underlying() => process_.x0();

        private double volatility() => process_.blackVolatility().link.blackVol(residualTime(), strike());
    }
}
