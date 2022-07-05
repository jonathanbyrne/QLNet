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
using QLNet.processes;

namespace QLNet.Pricingengines.Lookback
{
    //! Pricing engine for European continuous partial-time floating-strike lookback option
    /*! Formula from "Option Pricing Formulas, Second Edition",
        E.G. Haug, 2006, p.146

    */
    [PublicAPI]
    public class AnalyticContinuousPartialFloatingLookbackEngine : ContinuousPartialFloatingLookbackOption.Engine
    {
        private CumulativeNormalDistribution f_ = new CumulativeNormalDistribution();
        private GeneralizedBlackScholesProcess process_;

        public AnalyticContinuousPartialFloatingLookbackEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            var payoff = arguments_.payoff as FloatingTypePayoff;
            Utils.QL_REQUIRE(payoff != null, () => "Non-floating payoff given");

            Utils.QL_REQUIRE(process_.x0() > 0.0, () => "negative or null underlying");

            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    results_.value = A(1);
                    break;
                case QLNet.Option.Type.Put:
                    results_.value = A(-1);
                    break;
                default:
                    Utils.QL_FAIL("Unknown ExerciseType");
                    break;
            }
        }

        private double A(double eta)
        {
            var fullLookbackPeriod = lookbackPeriodEndTime().IsEqual(residualTime());
            var carry = riskFreeRate() - dividendYield();
            var vol = volatility();
            var x = 2.0 * carry / (vol * vol);
            var s = underlying() / minmax();

            var ls = System.Math.Log(s);
            var d1 = ls / stdDeviation() + 0.5 * (x + 1.0) * stdDeviation();
            var d2 = d1 - stdDeviation();

            double e1 = 0, e2 = 0;
            if (!fullLookbackPeriod)
            {
                e1 = (carry + vol * vol / 2) * (residualTime() - lookbackPeriodEndTime()) / (vol * System.Math.Sqrt(residualTime() - lookbackPeriodEndTime()));
                e2 = e1 - vol * System.Math.Sqrt(residualTime() - lookbackPeriodEndTime());
            }

            var f1 = (ls + (carry + vol * vol / 2) * lookbackPeriodEndTime()) / (vol * System.Math.Sqrt(lookbackPeriodEndTime()));
            var f2 = f1 - vol * System.Math.Sqrt(lookbackPeriodEndTime());

            var l1 = System.Math.Log(lambda()) / vol;
            var g1 = l1 / System.Math.Sqrt(residualTime());
            double g2 = 0;
            if (!fullLookbackPeriod)
            {
                g2 = l1 / System.Math.Sqrt(residualTime() - lookbackPeriodEndTime());
            }

            var n1 = f_.value(eta * (d1 - g1));
            var n2 = f_.value(eta * (d2 - g1));

            BivariateCumulativeNormalDistributionWe04DP cnbn1 = new BivariateCumulativeNormalDistributionWe04DP(1),
                cnbn2 = new BivariateCumulativeNormalDistributionWe04DP(0),
                cnbn3 = new BivariateCumulativeNormalDistributionWe04DP(-1);
            if (!fullLookbackPeriod)
            {
                cnbn1 = new BivariateCumulativeNormalDistributionWe04DP(System.Math.Sqrt(lookbackPeriodEndTime() / residualTime()));
                cnbn2 = new BivariateCumulativeNormalDistributionWe04DP(-System.Math.Sqrt(1 - lookbackPeriodEndTime() / residualTime()));
                cnbn3 = new BivariateCumulativeNormalDistributionWe04DP(-System.Math.Sqrt(lookbackPeriodEndTime() / residualTime()));
            }

            var n3 = cnbn1.value(eta * (-f1 + 2.0 * carry * System.Math.Sqrt(lookbackPeriodEndTime()) / vol), eta * (-d1 + x * stdDeviation() - g1));
            double n4 = 0, n5 = 0, n6 = 0, n7 = 0;
            if (!fullLookbackPeriod)
            {
                n4 = cnbn2.value(-eta * (d1 + g1), eta * (e1 + g2));
                n5 = cnbn2.value(-eta * (d1 - g1), eta * (e1 - g2));
                n6 = cnbn3.value(eta * -f2, eta * (d2 - g1));
                n7 = f_.value(eta * (e2 - g2));
            }
            else
            {
                n4 = f_.value(-eta * (d1 + g1));
            }

            var n8 = f_.value(-eta * f1);
            var pow_s = System.Math.Pow(s, -x);
            var pow_l = System.Math.Pow(lambda(), x);

            if (!fullLookbackPeriod)
            {
                return eta * (underlying() * dividendDiscount() * n1 -
                              lambda() * minmax() * riskFreeDiscount() * n2 +
                              underlying() * riskFreeDiscount() * lambda() / x *
                              (pow_s * n3 - dividendDiscount() / riskFreeDiscount() * pow_l * n4)
                              + underlying() * dividendDiscount() * n5 +
                              riskFreeDiscount() * lambda() * minmax() * n6 -
                              System.Math.Exp(-carry * (residualTime() - lookbackPeriodEndTime())) *
                              dividendDiscount() * (1 + 0.5 * vol * vol / carry) * lambda() *
                              underlying() * n7 * n8);
            }

            //Simpler calculation
            return eta * (underlying() * dividendDiscount() * n1 -
                          lambda() * minmax() * riskFreeDiscount() * n2 +
                          underlying() * riskFreeDiscount() * lambda() / x *
                          (pow_s * n3 - dividendDiscount() / riskFreeDiscount() * pow_l * n4));
        }

        private double dividendDiscount() => process_.dividendYield().link.discount(residualTime());

        private double dividendYield() =>
            process_.dividendYield().link.zeroRate(residualTime(),
                Compounding.Continuous, Frequency.NoFrequency).value();

        private double lambda() => arguments_.lambda;

        private double lookbackPeriodEndTime() => process_.time(arguments_.lookbackPeriodEnd);

        private double minmax() => arguments_.minmax.GetValueOrDefault();

        private double residualTime() => process_.time(arguments_.exercise.lastDate());

        private double riskFreeDiscount() => process_.riskFreeRate().link.discount(residualTime());

        private double riskFreeRate() =>
            process_.riskFreeRate().link.zeroRate(residualTime(), Compounding.Continuous,
                Frequency.NoFrequency).value();

        private double stdDeviation() => volatility() * System.Math.Sqrt(residualTime());

        // helper methods
        private double underlying() => process_.x0();

        private double volatility() => process_.blackVolatility().link.blackVol(residualTime(), minmax());
    }
}
