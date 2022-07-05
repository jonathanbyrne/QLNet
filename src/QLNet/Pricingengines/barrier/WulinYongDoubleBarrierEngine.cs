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
using QLNet.Pricingengines.vanilla;
using QLNet.processes;

namespace QLNet.Pricingengines.barrier
{
    //! Pricing engine for barrier options using analytical formulae
    /*! The formulas are taken from "Barrier Option Pricing",
         Wulin Suo, Yong Wang.

        \ingroup barrierengines

        \test the correctness of the returned value is tested by
              reproducing results available in literature.
    */
    [PublicAPI]
    public class WulinYongDoubleBarrierEngine : DoubleBarrierOption.Engine
    {
        private CumulativeNormalDistribution f_;
        private GeneralizedBlackScholesProcess process_;
        private int series_;

        public WulinYongDoubleBarrierEngine(GeneralizedBlackScholesProcess process, int series = 5)
        {
            process_ = process;
            series_ = series;
            f_ = new CumulativeNormalDistribution();

            process_.registerWith(update);
        }

        public override void calculate()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");
            Utils.QL_REQUIRE(payoff.strike() > 0.0, () => "strike must be positive");

            var K = payoff.strike();
            var S = process_.x0();
            Utils.QL_REQUIRE(S >= 0.0, () => "negative or null underlying given");
            Utils.QL_REQUIRE(!triggered(S), () => "barrier touched");

            var barrierType = arguments_.barrierType;
            Utils.QL_REQUIRE(barrierType == DoubleBarrier.Type.KnockOut ||
                             barrierType == DoubleBarrier.Type.KnockIn, () =>
                "only KnockIn and KnockOut options supported");

            var L = arguments_.barrier_lo.GetValueOrDefault();
            var H = arguments_.barrier_hi.GetValueOrDefault();
            var K_up = System.Math.Min(H, K);
            var K_down = System.Math.Max(L, K);
            var T = residualTime();
            var rd = riskFreeRate();
            var dd = riskFreeDiscount();
            var rf = dividendYield();
            var df = dividendDiscount();
            var vol = volatility();
            var mu = rd - rf - vol * vol / 2.0;
            var sgn = mu > 0 ? 1.0 : mu < 0 ? -1.0 : 0.0;
            //rebate
            var R_L = arguments_.rebate.GetValueOrDefault();
            var R_H = arguments_.rebate.GetValueOrDefault();

            //european option
            var europeanOption = new EuropeanOption(payoff, arguments_.exercise);
            IPricingEngine analyticEuropeanEngine = new AnalyticEuropeanEngine(process_);
            europeanOption.setPricingEngine(analyticEuropeanEngine);
            var european = europeanOption.NPV();

            double barrierOut = 0;
            double rebateIn = 0;
            for (var n = -series_; n < series_; n++)
            {
                var d1 = D(S / H * System.Math.Pow(L / H, 2.0 * n), vol * vol + mu, vol, T);
                var d2 = d1 - vol * System.Math.Sqrt(T);
                var g1 = D(H / S * System.Math.Pow(L / H, 2.0 * n - 1.0), vol * vol + mu, vol, T);
                var g2 = g1 - vol * System.Math.Sqrt(T);
                var h1 = D(S / H * System.Math.Pow(L / H, 2.0 * n - 1.0), vol * vol + mu, vol, T);
                var h2 = h1 - vol * System.Math.Sqrt(T);
                var k1 = D(L / S * System.Math.Pow(L / H, 2.0 * n - 1.0), vol * vol + mu, vol, T);
                var k2 = k1 - vol * System.Math.Sqrt(T);
                var d1_down = D(S / K_down * System.Math.Pow(L / H, 2.0 * n), vol * vol + mu, vol, T);
                var d2_down = d1_down - vol * System.Math.Sqrt(T);
                var d1_up = D(S / K_up * System.Math.Pow(L / H, 2.0 * n), vol * vol + mu, vol, T);
                var d2_up = d1_up - vol * System.Math.Sqrt(T);
                var k1_down = D(H * H / (K_down * S) * System.Math.Pow(L / H, 2.0 * n), vol * vol + mu, vol, T);
                var k2_down = k1_down - vol * System.Math.Sqrt(T);
                var k1_up = D(H * H / (K_up * S) * System.Math.Pow(L / H, 2.0 * n), vol * vol + mu, vol, T);
                var k2_up = k1_up - vol * System.Math.Sqrt(T);

                if (payoff.optionType() == QLNet.Option.Type.Call)
                {
                    barrierOut += System.Math.Pow(L / H, 2.0 * n * mu / (vol * vol)) *
                                  (df * S * System.Math.Pow(L / H, 2.0 * n) * (f_.value(d1_down) - f_.value(d1))
                                   - dd * K * (f_.value(d2_down) - f_.value(d2))
                                   - df * System.Math.Pow(L / H, 2.0 * n) * H * H / S * System.Math.Pow(H / S, 2.0 * mu / (vol * vol)) * (f_.value(k1_down) - f_.value(k1))
                                   + dd * K * System.Math.Pow(H / S, 2.0 * mu / (vol * vol)) * (f_.value(k2_down) - f_.value(k2)));
                }
                else if (payoff.optionType() == QLNet.Option.Type.Put)
                {
                    barrierOut += System.Math.Pow(L / H, 2.0 * n * mu / (vol * vol)) *
                                  (dd * K * (f_.value(h2) - f_.value(d2_up))
                                   - df * S * System.Math.Pow(L / H, 2.0 * n) * (f_.value(h1) - f_.value(d1_up))
                                   - dd * K * System.Math.Pow(H / S, 2.0 * mu / (vol * vol)) * (f_.value(g2) - f_.value(k2_up))
                                   + df * System.Math.Pow(L / H, 2.0 * n) * H * H / S * System.Math.Pow(H / S, 2.0 * mu / (vol * vol)) * (f_.value(g1) - f_.value(k1_up)));
                }
                else
                {
                    Utils.QL_FAIL("option ExerciseType not recognized");
                }

                var v1 = D(H / S * System.Math.Pow(H / L, 2.0 * n), -mu, vol, T);
                var v2 = D(H / S * System.Math.Pow(H / L, 2.0 * n), mu, vol, T);
                var v3 = D(S / L * System.Math.Pow(H / L, 2.0 * n), -mu, vol, T);
                var v4 = D(S / L * System.Math.Pow(H / L, 2.0 * n), mu, vol, T);
                rebateIn += dd * R_H * sgn * (System.Math.Pow(L / H, 2.0 * n * mu / (vol * vol)) * f_.value(sgn * v1) - System.Math.Pow(H / S, 2.0 * mu / (vol * vol)) * f_.value(-sgn * v2))
                            + dd * R_L * sgn * (System.Math.Pow(L / S, 2.0 * mu / (vol * vol)) * f_.value(-sgn * v3) - System.Math.Pow(H / L, 2.0 * n * mu / (vol * vol)) * f_.value(sgn * v4));
            }

            //rebate paid at maturity
            if (barrierType == DoubleBarrier.Type.KnockOut)
            {
                results_.value = barrierOut;
            }
            else
            {
                results_.value = european - barrierOut;
            }

            results_.additionalResults["vanilla"] = european;
            results_.additionalResults["barrierOut"] = barrierOut;
            results_.additionalResults["barrierIn"] = european - barrierOut;
        }

        private double D(double X, double lambda, double sigma, double T) => (System.Math.Log(X) + lambda * T) / (sigma * System.Math.Sqrt(T));

        private double dividendDiscount() => process_.dividendYield().link.discount(residualTime());

        private double dividendYield() =>
            process_.dividendYield().link.zeroRate(
                residualTime(), Compounding.Continuous, Frequency.NoFrequency).value();

        private double residualTime() => process_.time(arguments_.exercise.lastDate());

        private double riskFreeDiscount() => process_.riskFreeRate().link.discount(residualTime());

        private double riskFreeRate() =>
            process_.riskFreeRate().link.zeroRate(
                residualTime(), Compounding.Continuous, Frequency.NoFrequency).value();

        private double strike()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");
            return payoff.strike();
        }

        // helper methods
        private double underlying() => process_.x0();

        private double volatility() => process_.blackVolatility().link.blackVol(residualTime(), strike());
    }
}
