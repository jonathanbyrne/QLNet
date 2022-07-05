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
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.Processes;

namespace QLNet.PricingEngines.Lookback
{
    //! Pricing engine for European continuous floating-strike lookback
    /*! Formula from "Option Pricing Formulas",
        E.G. Haug, McGraw-Hill, 1998, p.61-62
    */
    [PublicAPI]
    public class AnalyticContinuousFloatingLookbackEngine : ContinuousFloatingLookbackOption.Engine
    {
        private CumulativeNormalDistribution f_ = new CumulativeNormalDistribution();
        private GeneralizedBlackScholesProcess process_;

        public AnalyticContinuousFloatingLookbackEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            var payoff = arguments_.payoff as FloatingTypePayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "Non-floating payoff given");

            QLNet.Utils.QL_REQUIRE(process_.x0() > 0.0, () => "negative or null underlying");

            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    results_.value = A(1);
                    break;
                case QLNet.Option.Type.Put:
                    results_.value = A(-1);
                    break;
                default:
                    QLNet.Utils.QL_FAIL("Unknown ExerciseType");
                    break;
            }
        }

        private double A(double eta)
        {
            var vol = volatility();
            var lambda = 2.0 * (riskFreeRate() - dividendYield()) / (vol * vol);
            var s = underlying() / minmax();
            var d1 = System.Math.Log(s) / stdDeviation() + 0.5 * (lambda + 1.0) * stdDeviation();
            var n1 = f_.value(eta * d1);
            var n2 = f_.value(eta * (d1 - stdDeviation()));
            var n3 = f_.value(eta * (-d1 + lambda * stdDeviation()));
            var n4 = f_.value(eta * -d1);
            var pow_s = System.Math.Pow(s, -lambda);
            return eta * (underlying() * dividendDiscount() * n1 -
                          minmax() * riskFreeDiscount() * n2 +
                          underlying() * riskFreeDiscount() *
                          (pow_s * n3 - dividendDiscount() * n4 / riskFreeDiscount()) / lambda);
        }

        private double dividendDiscount() => process_.dividendYield().link.discount(residualTime());

        private double dividendYield() =>
            process_.dividendYield().link.zeroRate(residualTime(),
                Compounding.Continuous, Frequency.NoFrequency).value();

        private double minmax() => arguments_.minmax.GetValueOrDefault();

        private double residualTime() => process_.time(arguments_.exercise.lastDate());

        private double riskFreeDiscount() => process_.riskFreeRate().link.discount(residualTime());

        private double riskFreeRate() =>
            process_.riskFreeRate().link.zeroRate(residualTime(),
                Compounding.Continuous, Frequency.NoFrequency).value();

        private double stdDeviation() => volatility() * System.Math.Sqrt(residualTime());

        // helper methods
        private double underlying() => process_.x0();

        private double volatility() => process_.blackVolatility().link.blackVol(residualTime(), minmax());
    }
}
