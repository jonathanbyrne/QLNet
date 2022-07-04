/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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

using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.Pricingengines;
using QLNet.processes;
using QLNet.Time;
using System;

namespace QLNet.Pricingengines.vanilla
{
    //! Pricing engine for American options with Ju quadratic approximation
    //    ! Reference:
    //        An Approximate Formula for Pricing American Options,
    //        Journal of Derivatives Winter 1999,
    //        Ju, N.
    //
    //        \warning Barone-Adesi-Whaley critical commodity price
    //                 calculation is used, it has not been modified to see
    //                 whether the method of Ju is faster. Ju does not say
    //                 how he solves the equation for the critical stock
    //                 price, e.g. Newton method. He just gives the
    //                 solution.  The method of BAW gives answers to the
    //                 same accuracy as in Ju (1999).
    //
    //        \ingroup vanillaengines
    //
    //        \test the correctness of the returned value is tested by
    //              reproducing results available in literature.
    //
    [JetBrains.Annotations.PublicAPI] public class JuQuadraticApproximationEngine : OneAssetOption.Engine
    {
        //     An Approximate Formula for Pricing American Options
        //        Journal of Derivatives Winter 1999
        //        Ju, N.
        //

        private GeneralizedBlackScholesProcess process_;

        public JuQuadraticApproximationEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.American, () => "not an American Option");

            var ex = arguments_.exercise as AmericanExercise;

            Utils.QL_REQUIRE(ex != null, () => "non-American exercise given");

            Utils.QL_REQUIRE(!ex.payoffAtExpiry(), () => "payoff at expiry not handled");

            var payoff = arguments_.payoff as StrikedTypePayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-striked payoff given");

            var variance = process_.blackVolatility().link.blackVariance(ex.lastDate(), payoff.strike());
            var dividendDiscount = process_.dividendYield().link.discount(ex.lastDate());
            var riskFreeDiscount = process_.riskFreeRate().link.discount(ex.lastDate());
            var spot = process_.stateVariable().link.value();

            Utils.QL_REQUIRE(spot > 0.0, () => "negative or null underlying given");

            var forwardPrice = spot * dividendDiscount / riskFreeDiscount;
            var black = new BlackCalculator(payoff, forwardPrice, System.Math.Sqrt(variance), riskFreeDiscount);

            if (dividendDiscount >= 1.0 && payoff.optionType() == QLNet.Option.Type.Call)
            {
                // early exercise never optimal
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
                results_.theta = black.theta(spot, t);
                results_.thetaPerDay = black.thetaPerDay(spot, t);

                results_.strikeSensitivity = black.strikeSensitivity();
                results_.itmCashProbability = black.itmCashProbability();
            }
            else
            {
                // early exercise can be optimal
                var cumNormalDist = new CumulativeNormalDistribution();
                var normalDist = new NormalDistribution();

                var tolerance = 1e-6;
                var Sk = BaroneAdesiWhaleyApproximationEngine.criticalPrice(payoff, riskFreeDiscount, dividendDiscount,
                                                                               variance, tolerance);

                var forwardSk = Sk * dividendDiscount / riskFreeDiscount;

                var alpha = -2.0 * System.Math.Log(riskFreeDiscount) / variance;
                var beta = 2.0 * System.Math.Log(dividendDiscount / riskFreeDiscount) / variance;
                var h = 1 - riskFreeDiscount;
                double phi = 0;
                switch (payoff.optionType())
                {
                    case QLNet.Option.Type.Call:
                        phi = 1;
                        break;
                    case QLNet.Option.Type.Put:
                        phi = -1;
                        break;
                    default:
                        Utils.QL_FAIL("invalid option ExerciseType");
                        break;
                }
                //it can throw: to be fixed
                // FLOATING_POINT_EXCEPTION
                var temp_root = System.Math.Sqrt((beta - 1) * (beta - 1) + 4 * alpha / h);
                var lambda = (-(beta - 1) + phi * temp_root) / 2;
                var lambda_prime = -phi * alpha / (h * h * temp_root);

                var black_Sk = Utils.blackFormula(payoff.optionType(), payoff.strike(), forwardSk, System.Math.Sqrt(variance)) *
                               riskFreeDiscount;
                var hA = phi * (Sk - payoff.strike()) - black_Sk;

                var d1_Sk = (System.Math.Log(forwardSk / payoff.strike()) + 0.5 * variance) / System.Math.Sqrt(variance);
                var d2_Sk = d1_Sk - System.Math.Sqrt(variance);
                var part1 = forwardSk * normalDist.value(d1_Sk) / (alpha * System.Math.Sqrt(variance));
                var part2 = -phi * forwardSk * cumNormalDist.value(phi * d1_Sk) * System.Math.Log(dividendDiscount) /
                            System.Math.Log(riskFreeDiscount);
                var part3 = +phi * payoff.strike() * cumNormalDist.value(phi * d2_Sk);
                var V_E_h = part1 + part2 + part3;

                var b = (1 - h) * alpha * lambda_prime / (2 * (2 * lambda + beta - 1));
                var c = -((1 - h) * alpha / (2 * lambda + beta - 1)) *
                        (V_E_h / hA + 1 / h + lambda_prime / (2 * lambda + beta - 1));
                var temp_spot_ratio = System.Math.Log(spot / Sk);
                var chi = temp_spot_ratio * (b * temp_spot_ratio + c);

                if (phi * (Sk - spot) > 0)
                {
                    results_.value = black.value() + hA * System.Math.Pow(spot / Sk, lambda) / (1 - chi);
                }
                else
                {
                    results_.value = phi * (spot - payoff.strike());
                }

                var temp_chi_prime = 2 * b / spot * System.Math.Log(spot / Sk);
                var chi_prime = temp_chi_prime + c / spot;
                var chi_double_prime = 2 * b / (spot * spot) - temp_chi_prime / spot - c / (spot * spot);
                results_.delta = phi * dividendDiscount * cumNormalDist.value(phi * d1_Sk) +
                                 (lambda / (spot * (1 - chi)) + chi_prime / ((1 - chi) * (1 - chi))) *
                                 (phi * (Sk - payoff.strike()) - black_Sk) * System.Math.Pow(spot / Sk, lambda);

                results_.gamma = phi * dividendDiscount * normalDist.value(phi * d1_Sk) / (spot * System.Math.Sqrt(variance)) +
                                 (2 * lambda * chi_prime / (spot * (1 - chi) * (1 - chi)) +
                                  2 * chi_prime * chi_prime / ((1 - chi) * (1 - chi) * (1 - chi)) +
                                  chi_double_prime / ((1 - chi) * (1 - chi)) +
                                  lambda * (1 - lambda) / (spot * spot * (1 - chi))) *
                                 (phi * (Sk - payoff.strike()) - black_Sk) * System.Math.Pow(spot / Sk, lambda);
            } // end of "early exercise can be optimal"
        }
    }

}
