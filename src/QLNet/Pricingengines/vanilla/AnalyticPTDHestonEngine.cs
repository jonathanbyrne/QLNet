﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Models.Equity;

namespace QLNet.PricingEngines.vanilla
{
    //! analytic piecewise constant time dependent Heston-model engine

    /*! References:

        Heston, Steven L., 1993. A Closed-Form Solution for Options
        with Stochastic Volatility with Applications to Bond and
        Currency Options.  The review of Financial Studies, Volume 6,
        Issue 2, 327-343.

        J. Gatheral, The Volatility Surface: A Practitioner's Guide,
        Wiley Finance

        A. Elices, Models with time-dependent parameters using
        transform methods: application to Heston’s model,
        http://arxiv.org/pdf/0708.2020

        \ingroup vanillaengines
    */
    [PublicAPI]
    public class AnalyticPTDHestonEngine : GenericModelEngine<PiecewiseTimeDependentHestonModel,
        QLNet.Option.Arguments, OneAssetOption.Results>
    {
        private class Fj_Helper
        {
            private readonly int j_;
            private readonly Handle<PiecewiseTimeDependentHestonModel> model_;
            private readonly List<double> r_;
            private readonly List<double> q_;
            private readonly double term_;
            private readonly TimeGrid timeGrid_;
            private readonly double v0_;
            private readonly double x_;
            private readonly double sx_;

            public Fj_Helper(Handle<PiecewiseTimeDependentHestonModel> model, double term, double strike, int j)
            {
                j_ = j;
                term_ = term;
                v0_ = model.link.v0();
                x_ = System.Math.Log(model.link.s0());
                sx_ = System.Math.Log(strike);
                r_ = new InitializedList<double>(model.link.timeGrid().size() - 1);
                q_ = new InitializedList<double>(model.link.timeGrid().size() - 1);
                model_ = model;
                timeGrid_ = model.link.timeGrid();

                for (var i = 0; i < timeGrid_.size() - 1; ++i)
                {
                    var begin = System.Math.Min(term_, timeGrid_[i]);
                    var end = System.Math.Min(term_, timeGrid_[i + 1]);
                    r_[i] = model.link.riskFreeRate().link.forwardRate(begin, end,
                        Compounding.Continuous, Frequency.NoFrequency).rate();
                    q_[i] = model.link.dividendYield().link.forwardRate(begin, end,
                        Compounding.Continuous, Frequency.NoFrequency).rate();
                }

                QLNet.Utils.QL_REQUIRE(term_ < model_.link.timeGrid().Last(), () => "maturity is too large");
            }

            public double value(double phi)
            {
                // avoid numeric overflow for phi->0.
                phi = System.Math.Max(double.Epsilon, phi);

                Complex D = 0.0;
                Complex C = 0.0;

                for (var i = timeGrid_.size() - 1; i > 0; --i)
                {
                    var begin = timeGrid_[i - 1];
                    if (begin < term_)
                    {
                        var end = System.Math.Min(term_, timeGrid_[i]);
                        var tau = end - begin;
                        var t = 0.5 * (end + begin);

                        var rho = model_.link.rho(t);
                        var sigma = model_.link.sigma(t);
                        var kappa = model_.link.kappa(t);
                        var theta = model_.link.theta(t);

                        var sigma2 = sigma * sigma;
                        var t0 = kappa - (j_ == 1 ? rho * sigma : 0);
                        var rpsig = rho * sigma * phi;

                        var t1 = t0 + new Complex(0, -rpsig);
                        var d = Complex.Sqrt(t1 * t1 - sigma2 * phi * new Complex(-phi, j_ == 1 ? 1 : -1));
                        var g = (t1 - d) / (t1 + d);
                        var gt = (t1 - d - D * sigma2) / (t1 + d - D * sigma2);

                        D = (t1 + d) / sigma2 * (g - gt * Complex.Exp(-d * tau)) / (1.0 - gt * Complex.Exp(-d * tau));

                        var lng = Complex.Log((1.0 - gt * Complex.Exp(-d * tau)) / (1.0 - gt));

                        C = kappa * theta / sigma2 * ((t1 - d) * tau - 2.0 * lng)
                            + new Complex(0.0, phi * (r_[i - 1] - q_[i - 1]) * tau) + C;
                    }
                }

                return Complex.Exp(v0_ * D + C + new Complex(0.0, phi * (x_ - sx_))).Imaginary / phi;
            }
        }

        private AnalyticHestonEngine.Integration integration_;

        // Simple to use constructor: Using adaptive
        // Gauss-Lobatto integration and Gatheral's version of complex log.
        // Be aware: using a too large number for maxEvaluations might result
        // in a stack overflow as the Lobatto integration is a recursive
        // algorithm.
        public AnalyticPTDHestonEngine(PiecewiseTimeDependentHestonModel model, double relTolerance, int maxEvaluations)
            : base(model)
        {
            integration_ = AnalyticHestonEngine.Integration.gaussLobatto(relTolerance, null, maxEvaluations);
        }

        // Constructor using Laguerre integration
        // and Gatheral's version of complex log.
        public AnalyticPTDHestonEngine(PiecewiseTimeDependentHestonModel model, int integrationOrder = 144)
            : base(model)
        {
            integration_ = AnalyticHestonEngine.Integration.gaussLaguerre(integrationOrder);
        }

        public override void calculate()
        {
            // this is an european option pricer
            QLNet.Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () => "not an European option");

            // plain vanilla
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-striked payoff given");

            var v0 = model_.link.v0();
            var spotPrice = model_.link.s0();
            QLNet.Utils.QL_REQUIRE(spotPrice > 0.0, () => "negative or null underlying given");

            var strike = payoff.strike();
            var term = model_.link.riskFreeRate().link.dayCounter().yearFraction(
                model_.link.riskFreeRate().link.referenceDate(), arguments_.exercise.lastDate());
            var riskFreeDiscount = model_.link.riskFreeRate().link.discount(arguments_.exercise.lastDate());
            var dividendDiscount = model_.link.dividendYield().link.discount(arguments_.exercise.lastDate());

            //average values
            var timeGrid = model_.link.timeGrid();
            var n = timeGrid.size() - 1;
            double kappaAvg = 0.0, thetaAvg = 0.0, sigmaAvg = 0.0, rhoAvg = 0.0;

            for (var i = 1; i <= n; ++i)
            {
                var t = 0.5 * (timeGrid[i - 1] + timeGrid[i]);
                kappaAvg += model_.link.kappa(t);
                thetaAvg += model_.link.theta(t);
                sigmaAvg += model_.link.sigma(t);
                rhoAvg += model_.link.rho(t);
            }

            kappaAvg /= n;
            thetaAvg /= n;
            sigmaAvg /= n;
            rhoAvg /= n;

            var c_inf = System.Math.Min(10.0, System.Math.Max(0.0001,
                System.Math.Sqrt(1.0 - System.Math.Pow(rhoAvg, 2)) / sigmaAvg)) * (v0 + kappaAvg * thetaAvg * term);

            var p1 = integration_.calculate(c_inf,
                new Fj_Helper(model_, term, strike, 1).value) / Const.M_PI;

            var p2 = integration_.calculate(c_inf,
                new Fj_Helper(model_, term, strike, 2).value) / Const.M_PI;

            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    results_.value = spotPrice * dividendDiscount * (p1 + 0.5)
                                     - strike * riskFreeDiscount * (p2 + 0.5);
                    break;
                case QLNet.Option.Type.Put:
                    results_.value = spotPrice * dividendDiscount * (p1 - 0.5)
                                     - strike * riskFreeDiscount * (p2 - 0.5);
                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown option ExerciseType");
                    break;
            }
        }
    }
}
