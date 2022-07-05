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
    //! Bjerksund and Stensland pricing engine for American options (1993)
    /*! \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in literature.
    */
    [PublicAPI]
    public class BjerksundStenslandApproximationEngine : OneAssetOption.Engine
    {
        // helper functions
        private CumulativeNormalDistribution cumNormalDist = new CumulativeNormalDistribution();
        private GeneralizedBlackScholesProcess process_;

        public BjerksundStenslandApproximationEngine(GeneralizedBlackScholesProcess process)
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

            var payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var variance = process_.blackVolatility().link.blackVariance(ex.lastDate(), payoff.strike());
            var dividendDiscount = process_.dividendYield().link.discount(ex.lastDate());
            var riskFreeDiscount = process_.riskFreeRate().link.discount(ex.lastDate());

            var spot = process_.stateVariable().link.value();
            Utils.QL_REQUIRE(spot > 0.0, () => "negative or null underlying given");

            var strike = payoff.strike();

            if (payoff.optionType() == QLNet.Option.Type.Put)
            {
                // use put-call simmetry
                Utils.swap<double>(ref spot, ref strike);
                Utils.swap<double>(ref riskFreeDiscount, ref dividendDiscount);
                payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, strike);
            }

            if (dividendDiscount >= 1.0)
            {
                // early exercise is never optimal - use Black formula
                var forwardPrice = spot * dividendDiscount / riskFreeDiscount;
                var black = new BlackCalculator(payoff, forwardPrice, System.Math.Sqrt(variance), riskFreeDiscount);

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
                // early exercise can be optimal - use approximation
                results_.value = americanCallApproximation(spot, strike, riskFreeDiscount, dividendDiscount, variance);
            }
        }

        private double americanCallApproximation(double S, double X, double rfD, double dD, double variance)
        {
            var bT = System.Math.Log(dD / rfD);
            var rT = System.Math.Log(1.0 / rfD);

            var beta = 0.5 - bT / variance + System.Math.Sqrt(System.Math.Pow(bT / variance - 0.5, 2.0) + 2.0 * rT / variance);
            var BInfinity = beta / (beta - 1.0) * X;
            var B0 = System.Math.Max(X, rT / (rT - bT) * X);
            var ht = -(bT + 2.0 * System.Math.Sqrt(variance)) * B0 / (BInfinity - B0);

            // investigate what happen to I for dD->0.0
            var I = B0 + (BInfinity - B0) * (1 - System.Math.Exp(ht));
            Utils.QL_REQUIRE(I >= X, () => "Bjerksund-Stensland approximation not applicable to this set of parameters");
            if (S >= I)
            {
                return S - X;
            }

            // investigate what happen to alpha for dD->0.0
            var alpha = (I - X) * System.Math.Pow(I, -beta);
            return (I - X) * System.Math.Pow(S / I, beta)
                           * (1 - phi(S, beta, I, I, rT, bT, variance))
                   + S * phi(S, 1.0, I, I, rT, bT, variance)
                   - S * phi(S, 1.0, X, I, rT, bT, variance)
                   - X * phi(S, 0.0, I, I, rT, bT, variance)
                   + X * phi(S, 0.0, X, I, rT, bT, variance);
        }

        private double phi(double S, double gamma, double H, double I, double rT, double bT, double variance)
        {
            var lambda = -rT + gamma * bT + 0.5 * gamma * (gamma - 1.0) * variance;
            var d = -(System.Math.Log(S / H) + (bT + (gamma - 0.5) * variance)) / System.Math.Sqrt(variance);
            var kappa = 2.0 * bT / variance + (2.0 * gamma - 1.0);
            return System.Math.Exp(lambda) * (cumNormalDist.value(d)
                                              - System.Math.Pow(I / S, kappa) *
                                              cumNormalDist.value(d - 2.0 * System.Math.Log(I / S) / System.Math.Sqrt(variance)));
        }
    }
}
