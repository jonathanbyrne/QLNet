/*
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
using QLNet.Math.Distributions;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    //! Barone-Adesi and Whaley pricing engine for American options (1987)
    /*! \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in literature.
    */
    [PublicAPI]
    public class BaroneAdesiWhaleyApproximationEngine : OneAssetOption.Engine
    {
        private GeneralizedBlackScholesProcess process_;

        public BaroneAdesiWhaleyApproximationEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;

            process_.registerWith(update);
        }

        // critical commodity price
        public static double criticalPrice(StrikedTypePayoff payoff, double riskFreeDiscount, double dividendDiscount,
            double variance, double tolerance)
        {
            // Calculation of seed value, Si
            var n = 2.0 * System.Math.Log(dividendDiscount / riskFreeDiscount) / variance;
            var m = -2.0 * System.Math.Log(riskFreeDiscount) / variance;
            var bT = System.Math.Log(dividendDiscount / riskFreeDiscount);

            double qu, Su, h, Si = 0;
            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    qu = (-(n - 1.0) + System.Math.Sqrt((n - 1.0) * (n - 1.0) + 4.0 * m)) / 2.0;
                    Su = payoff.strike() / (1.0 - 1.0 / qu);
                    h = -(bT + 2.0 * System.Math.Sqrt(variance)) * payoff.strike() /
                        (Su - payoff.strike());
                    Si = payoff.strike() + (Su - payoff.strike()) *
                        (1.0 - System.Math.Exp(h));
                    break;
                case QLNet.Option.Type.Put:
                    qu = (-(n - 1.0) - System.Math.Sqrt((n - 1.0) * (n - 1.0) + 4.0 * m)) / 2.0;
                    Su = payoff.strike() / (1.0 - 1.0 / qu);
                    h = (bT - 2.0 * System.Math.Sqrt(variance)) * payoff.strike() /
                        (payoff.strike() - Su);
                    Si = Su + (payoff.strike() - Su) * System.Math.Exp(h);
                    break;
                default:
                    Utils.QL_FAIL("unknown option ExerciseType");
                    break;
            }

            // Newton Raphson algorithm for finding critical price Si
            double Q, LHS, RHS, bi;
            var forwardSi = Si * dividendDiscount / riskFreeDiscount;
            var d1 = (System.Math.Log(forwardSi / payoff.strike()) + 0.5 * variance) /
                     System.Math.Sqrt(variance);
            var cumNormalDist = new CumulativeNormalDistribution();
            var K = !Utils.close(riskFreeDiscount, 1.0, 1000)
                ? -2.0 * System.Math.Log(riskFreeDiscount)
                  / (variance * (1.0 - riskFreeDiscount))
                : 2.0 / variance;

            var temp = Utils.blackFormula(payoff.optionType(), payoff.strike(),
                forwardSi, System.Math.Sqrt(variance)) * riskFreeDiscount;
            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    Q = (-(n - 1.0) + System.Math.Sqrt((n - 1.0) * (n - 1.0) + 4 * K)) / 2;
                    LHS = Si - payoff.strike();
                    RHS = temp + (1 - dividendDiscount * cumNormalDist.value(d1)) * Si / Q;
                    bi = dividendDiscount * cumNormalDist.value(d1) * (1 - 1 / Q) +
                         (1 - dividendDiscount *
                             cumNormalDist.derivative(d1) / System.Math.Sqrt(variance)) / Q;
                    while (System.Math.Abs(LHS - RHS) / payoff.strike() > tolerance)
                    {
                        Si = (payoff.strike() + RHS - bi * Si) / (1 - bi);
                        forwardSi = Si * dividendDiscount / riskFreeDiscount;
                        d1 = (System.Math.Log(forwardSi / payoff.strike()) + 0.5 * variance)
                             / System.Math.Sqrt(variance);
                        LHS = Si - payoff.strike();
                        var temp2 = Utils.blackFormula(payoff.optionType(), payoff.strike(),
                            forwardSi, System.Math.Sqrt(variance)) * riskFreeDiscount;
                        RHS = temp2 + (1 - dividendDiscount * cumNormalDist.value(d1)) * Si / Q;
                        bi = dividendDiscount * cumNormalDist.value(d1) * (1 - 1 / Q)
                             + (1 - dividendDiscount *
                                 cumNormalDist.derivative(d1) / System.Math.Sqrt(variance))
                             / Q;
                    }

                    break;
                case QLNet.Option.Type.Put:
                    Q = (-(n - 1.0) - System.Math.Sqrt((n - 1.0) * (n - 1.0) + 4 * K)) / 2;
                    LHS = payoff.strike() - Si;
                    RHS = temp - (1 - dividendDiscount * cumNormalDist.value(-d1)) * Si / Q;
                    bi = -dividendDiscount * cumNormalDist.value(-d1) * (1 - 1 / Q)
                         - (1 + dividendDiscount * cumNormalDist.derivative(-d1)
                             / System.Math.Sqrt(variance)) / Q;
                    while (System.Math.Abs(LHS - RHS) / payoff.strike() > tolerance)
                    {
                        Si = (payoff.strike() - RHS + bi * Si) / (1 + bi);
                        forwardSi = Si * dividendDiscount / riskFreeDiscount;
                        d1 = (System.Math.Log(forwardSi / payoff.strike()) + 0.5 * variance)
                             / System.Math.Sqrt(variance);
                        LHS = payoff.strike() - Si;
                        var temp2 = Utils.blackFormula(payoff.optionType(), payoff.strike(),
                            forwardSi, System.Math.Sqrt(variance)) * riskFreeDiscount;
                        RHS = temp2 - (1 - dividendDiscount * cumNormalDist.value(-d1)) * Si / Q;
                        bi = -dividendDiscount * cumNormalDist.value(-d1) * (1 - 1 / Q)
                             - (1 + dividendDiscount * cumNormalDist.derivative(-d1)
                                 / System.Math.Sqrt(variance)) / Q;
                    }

                    break;
                default:
                    Utils.QL_FAIL("unknown option ExerciseType");
                    break;
            }

            return Si;
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
                var tolerance = 1e-6;
                var Sk = criticalPrice(payoff, riskFreeDiscount,
                    dividendDiscount, variance, tolerance);
                var forwardSk = Sk * dividendDiscount / riskFreeDiscount;
                var d1 = (System.Math.Log(forwardSk / payoff.strike()) + 0.5 * variance)
                         / System.Math.Sqrt(variance);
                var n = 2.0 * System.Math.Log(dividendDiscount / riskFreeDiscount) / variance;
                var K = !Utils.close(riskFreeDiscount, 1.0, 1000)
                    ? -2.0 * System.Math.Log(riskFreeDiscount)
                      / (variance * (1.0 - riskFreeDiscount))
                    : 2.0 / variance;
                double Q, a;
                switch (payoff.optionType())
                {
                    case QLNet.Option.Type.Call:
                        Q = (-(n - 1.0) + System.Math.Sqrt((n - 1.0) * (n - 1.0) + 4.0 * K)) / 2.0;
                        a = Sk / Q * (1.0 - dividendDiscount * cumNormalDist.value(d1));
                        if (spot < Sk)
                        {
                            results_.value = black.value() +
                                             a * System.Math.Pow(spot / Sk, Q);
                        }
                        else
                        {
                            results_.value = spot - payoff.strike();
                        }

                        break;
                    case QLNet.Option.Type.Put:
                        Q = (-(n - 1.0) - System.Math.Sqrt((n - 1.0) * (n - 1.0) + 4.0 * K)) / 2.0;
                        a = -(Sk / Q) *
                            (1.0 - dividendDiscount * cumNormalDist.value(-d1));
                        if (spot > Sk)
                        {
                            results_.value = black.value() +
                                             a * System.Math.Pow(spot / Sk, Q);
                        }
                        else
                        {
                            results_.value = payoff.strike() - spot;
                        }

                        break;
                    default:
                        Utils.QL_FAIL("unknown option ExerciseType");
                        break;
                }
            } // end of "early exercise can be optimal"
        }
    }
}
