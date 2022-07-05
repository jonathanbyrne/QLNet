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
    //! Pricing engine for European continuous fixed-strike lookback
    /*! Formula from "Option Pricing Formulas",
        E.G. Haug, McGraw-Hill, 1998, p.63-64
    */
    [PublicAPI]
    public class AnalyticContinuousFixedLookbackEngine : ContinuousFixedLookbackOption.Engine
    {
        private CumulativeNormalDistribution f_ = new CumulativeNormalDistribution();
        private GeneralizedBlackScholesProcess process_;

        public AnalyticContinuousFixedLookbackEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "Non-plain payoff given");

            QLNet.Utils.QL_REQUIRE(process_.x0() > 0.0, () => "negative or null underlying");

            var strike = payoff.strike();

            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    QLNet.Utils.QL_REQUIRE(payoff.strike() >= 0.0, () => "Strike must be positive or null");
                    if (strike <= minmax())
                    {
                        results_.value = A(1) + C(1);
                    }
                    else
                    {
                        results_.value = B(1);
                    }

                    break;
                case QLNet.Option.Type.Put:
                    QLNet.Utils.QL_REQUIRE(payoff.strike() > 0.0, () => "Strike must be positive");
                    if (strike >= minmax())
                    {
                        results_.value = A(-1) + C(-1);
                    }
                    else
                    {
                        results_.value = B(-1);
                    }

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
            var ss = underlying() / minmax();
            var d1 = System.Math.Log(ss) / stdDeviation() + 0.5 * (lambda + 1.0) * stdDeviation();
            var N1 = f_.value(eta * d1);
            var N2 = f_.value(eta * (d1 - stdDeviation()));
            var N3 = f_.value(eta * (d1 - lambda * stdDeviation()));
            var N4 = f_.value(eta * d1);
            var powss = System.Math.Pow(ss, -lambda);
            return eta * (underlying() * dividendDiscount() * N1 -
                          minmax() * riskFreeDiscount() * N2 -
                          underlying() * riskFreeDiscount() *
                          (powss * N3 - dividendDiscount() * N4 / riskFreeDiscount()) / lambda);
        }

        private double B(double eta)
        {
            var vol = volatility();
            var lambda = 2.0 * (riskFreeRate() - dividendYield()) / (vol * vol);
            var ss = underlying() / strike();
            var d1 = System.Math.Log(ss) / stdDeviation() + 0.5 * (lambda + 1.0) * stdDeviation();
            var N1 = f_.value(eta * d1);
            var N2 = f_.value(eta * (d1 - stdDeviation()));
            var N3 = f_.value(eta * (d1 - lambda * stdDeviation()));
            var N4 = f_.value(eta * d1);
            var powss = System.Math.Pow(ss, -lambda);
            return eta * (underlying() * dividendDiscount() * N1 -
                          strike() * riskFreeDiscount() * N2 -
                          underlying() * riskFreeDiscount() *
                          (powss * N3 - dividendDiscount() * N4 / riskFreeDiscount()) / lambda);
        }

        private double C(double eta) => eta * (riskFreeDiscount() * (minmax() - strike()));

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
