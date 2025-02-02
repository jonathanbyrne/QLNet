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

using System;
using System.Numerics;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Math.integrals;
using QLNet.Models.Equity;

namespace QLNet.PricingEngines.vanilla
{
    //! analytic Heston-model engine based on Fourier transform
    /*! Integration detail:
        Two algebraically equivalent formulations of the complex
        logarithm of the Heston model exist. Gatherals [2005]
        (also Duffie, Pan and Singleton [2000], and Schoutens,
        Simons and Tistaert[2004]) version does not cause
        discoutinuities whereas the original version (e.g. Heston [1993])
        needs some sort of "branch correction" to work properly.
        Gatheral's version does also work with adaptive integration
        routines and should be preferred over the original Heston version.
    */

    /*! References:

        Heston, Steven L., 1993. A Closed-Form Solution for Options
        with Stochastic Volatility with Applications to Bond and
        Currency Options.  The review of Financial Studies, Volume 6,
        Issue 2, 327-343.

        A. Sepp, Pricing European-Style Options under Jump Diffusion
        Processes with Stochastic Volatility: Applications of Fourier
        Transform (<http://math.ut.ee/~spartak/papers/stochjumpvols.pdf>)

        R. Lord and C. Kahl, Why the rotation count algorithm works,
        http://papers.ssrn.com/sol3/papers.cfm?abstract_id=921335

        H. Albrecher, P. Mayer, W.Schoutens and J. Tistaert,
        The Little Heston Trap, http://www.schoutens.be/HestonTrap.pdf

        J. Gatheral, The Volatility Surface: A Practitioner's Guide,
        Wiley Finance

        \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in web/literature
              and comparison with Black pricing.
    */
    [PublicAPI]
    public class AnalyticHestonEngine : GenericModelEngine<HestonModel, QLNet.Option.Arguments, OneAssetOption.Results>
    {
        public enum ComplexLogFormula
        {
            Gatheral,
            BranchCorrection
        }

        [PublicAPI]
        public class Integration
        {
            private enum Algorithm
            {
                GaussLobatto,
                GaussKronrod,
                Simpson,
                Trapezoid,
                GaussLaguerre,
                GaussLegendre,
                GaussChebyshev,
                GaussChebyshev2nd
            }

            private GaussianQuadrature gaussianQuadrature_;
            private Algorithm intAlgo_;
            private Integrator integrator_;

            private Integration(Algorithm intAlgo, GaussianQuadrature gaussianQuadrature)
            {
                intAlgo_ = intAlgo;
                gaussianQuadrature_ = gaussianQuadrature;
            }

            private Integration(Algorithm intAlgo, Integrator integrator)
            {
                intAlgo_ = intAlgo;
                integrator_ = integrator;
            }

            public static Integration gaussChebyshev(int intOrder = 128) => new Integration(Algorithm.GaussChebyshev, new GaussChebyshevIntegration(intOrder));

            public static Integration gaussChebyshev2nd(int intOrder = 128) => new Integration(Algorithm.GaussChebyshev2nd, new GaussChebyshev2ndIntegration(intOrder));

            // usually these routines have a poor convergence behavior.
            public static Integration gaussKronrod(double absTolerance, int maxEvaluations = 1000) => new Integration(Algorithm.GaussKronrod, new GaussKronrodAdaptive(absTolerance, maxEvaluations));

            // non adaptive integration algorithms based on Gaussian quadrature
            public static Integration gaussLaguerre(int intOrder = 128)
            {
                QLNet.Utils.QL_REQUIRE(intOrder <= 192, () => "maximum integraton order (192) exceeded");
                return new Integration(Algorithm.GaussLaguerre, new GaussLaguerreIntegration(intOrder));
            }

            public static Integration gaussLegendre(int intOrder = 128) => new Integration(Algorithm.GaussLegendre, new GaussLegendreIntegration(intOrder));

            // for an adaptive integration algorithm Gatheral's version has to
            // be used.Be aware: using a too large number for maxEvaluations might
            // result in a stack overflow as the these integrations are based on
            // recursive algorithms.
            public static Integration gaussLobatto(double relTolerance, double? absTolerance, int maxEvaluations = 1000) =>
                new Integration(Algorithm.GaussLobatto, new GaussLobattoIntegral(maxEvaluations,
                    absTolerance, relTolerance, false));

            public static Integration simpson(double absTolerance, int maxEvaluations = 1000) => new Integration(Algorithm.Simpson, new SimpsonIntegral(absTolerance, maxEvaluations));

            public static Integration trapezoid(double absTolerance, int maxEvaluations = 1000) => new Integration(Algorithm.Trapezoid, new TrapezoidIntegral<Default>(absTolerance, maxEvaluations));

            public double calculate(double c_inf, Func<double, double> f)
            {
                double retVal = 0;

                switch (intAlgo_)
                {
                    case Algorithm.GaussLaguerre:
                        retVal = gaussianQuadrature_.value(f);
                        break;
                    case Algorithm.GaussLegendre:
                    case Algorithm.GaussChebyshev:
                    case Algorithm.GaussChebyshev2nd:
                        retVal = gaussianQuadrature_.value(new integrand1(c_inf, f).value);
                        break;
                    case Algorithm.Simpson:
                    case Algorithm.Trapezoid:
                    case Algorithm.GaussLobatto:
                    case Algorithm.GaussKronrod:
                        retVal = integrator_.value(new integrand2(c_inf, f).value, 0.0, 1.0);
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unknwon integration algorithm");
                        break;
                }

                return retVal;
            }

            public bool isAdaptiveIntegration() =>
                intAlgo_ == Algorithm.GaussLobatto
                || intAlgo_ == Algorithm.GaussKronrod
                || intAlgo_ == Algorithm.Simpson
                || intAlgo_ == Algorithm.Trapezoid;

            public int numberOfEvaluations()
            {
                if (integrator_ != null)
                {
                    return integrator_.numberOfEvaluations();
                }

                if (gaussianQuadrature_ != null)
                {
                    return gaussianQuadrature_.order();
                }

                QLNet.Utils.QL_FAIL("neither Integrator nor GaussianQuadrature given");
                return 0; // jfc
            }
        }

        private class Fj_Helper
        {
            // log branch counter
            private int b_; // log branch counter
            private readonly ComplexLogFormula cpxLog_;
            private readonly AnalyticHestonEngine engine_;
            private double g_km1_; // imag part of last log value
            private readonly int j_;
            private readonly double kappa_;
            private readonly double theta_;
            private readonly double sigma_;
            private readonly double v0_;
            private readonly double sigma2_;
            private readonly double rsigma_;
            private readonly double t0_;

            // helper variables
            private readonly double term_;
            private readonly double x_;
            private readonly double sx_;
            private readonly double dd_;

            public Fj_Helper(QLNet.Option.Arguments arguments,
                HestonModel model,
                AnalyticHestonEngine engine,
                ComplexLogFormula cpxLog,
                double term, double ratio, int j)
            {
                j_ = j; //arg_(arguments),
                kappa_ = model.kappa();
                theta_ = model.theta();
                sigma_ = model.sigma();
                v0_ = model.v0();
                cpxLog_ = cpxLog;
                term_ = term;
                x_ = System.Math.Log(model.process().s0().link.value());
                sx_ = System.Math.Log(((StrikedTypePayoff)arguments.payoff).strike());
                dd_ = x_ - System.Math.Log(ratio);
                sigma2_ = sigma_ * sigma_;
                rsigma_ = model.rho() * sigma_;
                t0_ = kappa_ - (j_ == 1 ? model.rho() * sigma_ : 0);
                b_ = 0;
                g_km1_ = 0;
                engine_ = engine;
            }

            public Fj_Helper(double kappa, double theta, double sigma,
                double v0, double s0, double rho,
                AnalyticHestonEngine engine,
                ComplexLogFormula cpxLog,
                double term,
                double strike,
                double ratio,
                int j)
            {
                j_ = j;
                kappa_ = kappa;
                theta_ = theta;
                sigma_ = sigma;
                v0_ = v0;
                cpxLog_ = cpxLog;
                term_ = term;
                x_ = System.Math.Log(s0);
                sx_ = System.Math.Log(strike);
                dd_ = x_ - System.Math.Log(ratio);
                sigma2_ = sigma_ * sigma_;
                rsigma_ = rho * sigma_;
                t0_ = kappa - (j == 1 ? rho * sigma : 0);
                b_ = 0;
                g_km1_ = 0;
                engine_ = engine;
            }

            public Fj_Helper(double kappa, double theta, double sigma,
                double v0, double s0, double rho,
                ComplexLogFormula cpxLog,
                double term,
                double strike,
                double ratio,
                int j)
            {
                j_ = j;
                kappa_ = kappa;
                theta_ = theta;
                sigma_ = sigma;
                v0_ = v0;
                cpxLog_ = cpxLog;
                term_ = term;
                x_ = System.Math.Log(s0);
                sx_ = System.Math.Log(strike);
                dd_ = x_ - System.Math.Log(ratio);
                sigma2_ = sigma_ * sigma_;
                rsigma_ = rho * sigma_;
                t0_ = kappa - (j == 1 ? rho * sigma : 0);
                b_ = 0;
                g_km1_ = 0;
                engine_ = null;
            }

            public double value(double phi)
            {
                var rpsig = rsigma_ * phi;

                var t1 = t0_ + new Complex(0, -rpsig);
                var d = Complex.Sqrt(t1 * t1 - sigma2_ * phi * new Complex(-phi, j_ == 1 ? 1 : -1));
                var ex = Complex.Exp(-d * term_);
                var addOnTerm = engine_ != null ? engine_.addOnTerm(phi, term_, j_) : 0.0;

                if (cpxLog_ == ComplexLogFormula.Gatheral)
                {
                    if (phi.IsNotEqual(0.0))
                    {
                        if (sigma_ > 1e-5)
                        {
                            var p = (t1 - d) / (t1 + d);
                            var g = Complex.Log((1.0 - p * ex) / (1.0 - p));

                            return Complex.Exp(v0_ * (t1 - d) * (1.0 - ex) / (sigma2_ * (1.0 - ex * p))
                                               + kappa_ * theta_ / sigma2_ * ((t1 - d) * term_ - 2.0 * g)
                                               + new Complex(0.0, phi * (dd_ - sx_))
                                               + addOnTerm
                            ).Imaginary / phi;
                        }
                        else
                        {
                            var td = phi / (2.0 * t1) * new Complex(-phi, j_ == 1 ? 1 : -1);
                            var p = td * sigma2_ / (t1 + d);
                            var g = p * (1.0 - ex);

                            return Complex.Exp(v0_ * td * (1.0 - ex) / (1.0 - p * ex)
                                               + kappa_ * theta_ * (td * term_ - 2.0 * g / sigma2_)
                                               + new Complex(0.0, phi * (dd_ - sx_))
                                               + addOnTerm
                            ).Imaginary / phi;
                        }
                    }

                    // use l'Hospital's rule
                    if (j_ == 1)
                    {
                        var kmr = rsigma_ - kappa_;
                        if (System.Math.Abs(kmr) > 1e-7)
                        {
                            return dd_ - sx_
                                   + (System.Math.Exp(kmr * term_) * kappa_ * theta_
                                      - kappa_ * theta_ * (kmr * term_ + 1.0)) / (2 * kmr * kmr)
                                   - v0_ * (1.0 - System.Math.Exp(kmr * term_)) / (2.0 * kmr);
                        }

                        return dd_ - sx_ + 0.25 * kappa_ * theta_ * term_ * term_
                                         + 0.5 * v0_ * term_;
                    }

                    return dd_ - sx_
                               - (System.Math.Exp(-kappa_ * term_) * kappa_ * theta_
                                  + kappa_ * theta_ * (kappa_ * term_ - 1.0)) / (2 * kappa_ * kappa_)
                               - v0_ * (1.0 - System.Math.Exp(-kappa_ * term_)) / (2 * kappa_);
                }

                if (cpxLog_ == ComplexLogFormula.BranchCorrection)
                {
                    var p = (t1 + d) / (t1 - d);

                    // next term: g = std::log((1.0 - p*std::exp(d*term_))/(1.0 - p))
                    var g = new Complex();

                    // the exp of the following expression is needed.
                    var e = Complex.Log(p) + d * term_;

                    // does it fit to the machine precision?
                    if (System.Math.Exp(-e.Real) > Const.QL_EPSILON)
                    {
                        g = Complex.Log((1.0 - p / ex) / (1.0 - p));
                    }
                    else
                    {
                        // use a "big phi" approximation
                        g = d * term_ + Complex.Log(p / (p - 1.0));

                        if (g.Imaginary > Const.M_PI || g.Imaginary <= -Const.M_PI)
                        {
                            // get back to principal branch of the complex logarithm
                            var im = g.Imaginary - 2 * Const.M_PI * System.Math.Floor(g.Imaginary / 2 * Const.M_PI);
                            if (im > Const.M_PI)
                            {
                                im -= 2 * Const.M_PI;
                            }
                            else if (im <= -Const.M_PI)
                            {
                                im += 2 * Const.M_PI;
                            }

                            g = new Complex(g.Real, im);
                        }
                    }

                    // be careful here as we have to use a log branch correction
                    // to deal with the discontinuities of the complex logarithm.
                    // the principal branch is not always the correct one.
                    // (s. A. Sepp, chapter 4)
                    // remark: there is still the change that we miss a branch
                    // if the order of the integration is not high enough.
                    var tmp = g.Imaginary - g_km1_;
                    if (tmp <= -Const.M_PI)
                    {
                        ++b_;
                    }
                    else if (tmp > Const.M_PI)
                    {
                        --b_;
                    }

                    g_km1_ = g.Imaginary;
                    g += new Complex(0, 2 * b_ * Const.M_PI);

                    return Complex.Exp(v0_ * (t1 + d) * (ex - 1.0) / (sigma2_ * (ex - p))
                                       + kappa_ * theta_ / sigma2_ * ((t1 + d) * term_ - 2.0 * g)
                                       + new Complex(0, phi * (dd_ - sx_))
                                       + addOnTerm
                    ).Imaginary / phi;
                }

                QLNet.Utils.QL_FAIL("unknown complex logarithm formula");
                return 0;
            }
        }

        private class integrand1
        {
            private readonly double c_inf;
            private readonly Func<double, double> f;

            public integrand1(double _c_inf, Func<double, double> _f)
            {
                c_inf = _c_inf;
                f = _f;
            }

            public double value(double x)
            {
                if ((x + 1.0) * c_inf > Const.QL_EPSILON)
                {
                    return f(-System.Math.Log(0.5 * x + 0.5) / c_inf) / ((x + 1.0) * c_inf);
                }

                return 0.0;
            }
        }

        private class integrand2
        {
            private readonly double c_inf;
            private readonly Func<double, double> f;

            public integrand2(double _c_inf, Func<double, double> _f)
            {
                c_inf = _c_inf;
                f = _f;
            }

            public double value(double x)
            {
                if (x * c_inf > Const.QL_EPSILON)
                {
                    return f(-System.Math.Log(x) / c_inf) / (x * c_inf);
                }

                return 0.0;
            }
        }

        private ComplexLogFormula cpxLog_;
        private int evaluations_;
        private Integration integration_;

        // Simple to use constructor: Using adaptive
        // Gauss-Lobatto integration and Gatheral's version of complex log.
        // Be aware: using a too large number for maxEvaluations might result
        // in a stack overflow as the Lobatto integration is a recursive
        // algorithm.
        public AnalyticHestonEngine(HestonModel model, double relTolerance, int maxEvaluations)
            : base(model)
        {
            evaluations_ = 0;
            cpxLog_ = ComplexLogFormula.Gatheral;
            integration_ = Integration.gaussLobatto(relTolerance, null, maxEvaluations);
        }

        // Constructor using Laguerre integration
        // and Gatheral's version of complex log.
        public AnalyticHestonEngine(HestonModel model, int integrationOrder = 144)
            : base(model)
        {
            evaluations_ = 0;
            cpxLog_ = ComplexLogFormula.Gatheral;
            integration_ = Integration.gaussLaguerre(integrationOrder);
        }

        // Constructor giving full control
        // over the Fourier integration algorithm
        public AnalyticHestonEngine(HestonModel model, ComplexLogFormula cpxLog, Integration integration)
            : base(model)
        {
            evaluations_ = 0;
            cpxLog_ = cpxLog;
            integration_ = integration; // TODO check

            QLNet.Utils.QL_REQUIRE(cpxLog_ != ComplexLogFormula.BranchCorrection
                                   || !integration.isAdaptiveIntegration(), () =>
                "Branch correction does not work in conjunction with adaptive integration methods");
        }

        public static void doCalculation(double riskFreeDiscount,
            double dividendDiscount,
            double spotPrice,
            double strikePrice,
            double term,
            double kappa, double theta, double sigma, double v0, double rho,
            TypePayoff type,
            Integration integration,
            ComplexLogFormula cpxLog,
            AnalyticHestonEngine enginePtr,
            ref double? value,
            ref int evaluations)
        {
            var ratio = riskFreeDiscount / dividendDiscount;

            var c_inf = System.Math.Min(10.0, System.Math.Max(0.0001,
                            System.Math.Sqrt(1.0 - System.Math.Pow(rho, 2)) / sigma))
                        * (v0 + kappa * theta * term);

            evaluations = 0;
            var p1 = integration.calculate(c_inf,
                new Fj_Helper(kappa, theta, sigma, v0, spotPrice, rho, enginePtr,
                    cpxLog, term, strikePrice, ratio, 1).value) / Const.M_PI;
            evaluations += integration.numberOfEvaluations();

            var p2 = integration.calculate(c_inf,
                new Fj_Helper(kappa, theta, sigma, v0, spotPrice, rho, enginePtr,
                    cpxLog, term, strikePrice, ratio, 2).value) / Const.M_PI;

            evaluations += integration.numberOfEvaluations();

            switch (type.optionType())
            {
                case QLNet.Option.Type.Call:
                    value = spotPrice * dividendDiscount * (p1 + 0.5) - strikePrice * riskFreeDiscount * (p2 + 0.5);
                    break;
                case QLNet.Option.Type.Put:
                    value = spotPrice * dividendDiscount * (p1 - 0.5) - strikePrice * riskFreeDiscount * (p2 - 0.5);
                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown option ExerciseType");
                    break;
            }
        }

        public override void calculate()
        {
            // this is a european option pricer
            QLNet.Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () => "not an European option");

            // plain vanilla
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non plain vanilla payoff given");

            var process = model_.link.process();

            var riskFreeDiscount = process.riskFreeRate().link.discount(arguments_.exercise.lastDate());
            var dividendDiscount = process.dividendYield().link.discount(arguments_.exercise.lastDate());

            var spotPrice = process.s0().link.value();
            QLNet.Utils.QL_REQUIRE(spotPrice > 0.0, () => "negative or null underlying given");

            var strikePrice = payoff.strike();
            var term = process.time(arguments_.exercise.lastDate());

            double? resultsValue = null;
            doCalculation(riskFreeDiscount,
                dividendDiscount,
                spotPrice,
                strikePrice,
                term,
                model_.link.kappa(),
                model_.link.theta(),
                model_.link.sigma(),
                model_.link.v0(),
                model_.link.rho(),
                payoff,
                integration_,
                cpxLog_,
                this,
                ref resultsValue,
                ref evaluations_);
            results_.value = resultsValue;
        }

        public int numberOfEvaluations() => evaluations_;

        // call back for extended stochastic volatility
        // plus jump diffusion engines like bates model
        protected virtual Complex addOnTerm(double phi, double t, int j) => new Complex(0, 0);
    }
}
