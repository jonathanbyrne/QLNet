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

using System.Numerics;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.Distributions;
using QLNet.Math.integrals;
using QLNet.Math.Solvers1d;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Time;
using Vector = QLNet.Math.Vector;

namespace QLNet.Processes
{
    //! Square-root stochastic-volatility Heston process
    // This class describes the square root stochastic volatility
    [PublicAPI]
    public class HestonProcess : StochasticProcess
    {
        public enum Discretization
        {
            PartialTruncation,
            FullTruncation,
            Reflection,
            NonCentralChiSquareVariance,
            QuadraticExponential,
            QuadraticExponentialMartingale,
            BroadieKayaExactSchemeLobatto,
            BroadieKayaExactSchemeLaguerre,
            BroadieKayaExactSchemeTrapezoidal
        }

        private class cdf_nu_ds : ISolver1d
        {
            private readonly Discretization discretization;
            private readonly double dt;
            private readonly double nu_0;
            private readonly double nu_t;
            private readonly HestonProcess process;

            public cdf_nu_ds(HestonProcess _process, double _nu_0, double _nu_t, double _dt,
                Discretization _discretization)
            {
                process = _process;
                nu_0 = _nu_0;
                nu_t = _nu_t;
                dt = _dt;
                discretization = _discretization;
            }

            public override double value(double x)
            {
                var eps = 1e-4;
                var u_eps = System.Math.Min(100.0,
                    System.Math.Max(0.1, cornishFisherEps(process, nu_0, nu_t, dt, eps)));

                switch (discretization)
                {
                    case Discretization.BroadieKayaExactSchemeLaguerre:
                    {
                        var gaussLaguerreIntegration = new GaussLaguerreIntegration(128);

                        // get the upper bound for the integration
                        var upper = u_eps / 2.0;
                        while (Complex.Abs(Phi(process, upper, nu_0, nu_t, dt) / upper) > eps)
                        {
                            upper *= 2.0;
                        }

                        return x < upper
                            ? System.Math.Max(0.0, System.Math.Min(1.0,
                                gaussLaguerreIntegration.value(new ch(process, x, nu_0, nu_t, dt).value)))
                            : 1.0;
                    }
                    case Discretization.BroadieKayaExactSchemeLobatto:
                    {
                        // get the upper bound for the integration
                        var upper = u_eps / 2.0;
                        while (Complex.Abs(Phi(process, upper, nu_0, nu_t, dt) / upper) > eps)
                        {
                            upper *= 2.0;
                        }

                        return x < upper
                            ? System.Math.Max(0.0, System.Math.Min(1.0,
                                new GaussLobattoIntegral(default, eps).value(new ch(process, x, nu_0, nu_t, dt).value, Const.QL_EPSILON, upper)))
                            : 1.0;
                    }
                    case Discretization.BroadieKayaExactSchemeTrapezoidal:
                    {
                        var h = 0.05;

                        var si = Si(0.5 * h * x);
                        var s = Const.M_2_PI * si;
                        var f = new Complex();
                        var j = 0;
                        do
                        {
                            ++j;
                            var u = h * j;
                            var si_n = Si(x * (u + 0.5 * h));

                            f = Phi(process, u, nu_0, nu_t, dt);
                            s += Const.M_2_PI * f.Real * (si_n - si);
                            si = si_n;
                        } while (Const.M_2_PI * Complex.Abs(f) / j > eps);

                        return s;
                    }
                    default:
                        QLNet.Utils.QL_FAIL("unknown integration method");
                        break;
                }

                return 0;
            }

            private double cornishFisherEps(HestonProcess process, double nu_0, double nu_t, double dt, double eps)
            {
                // use moment generating function to get the
                // first,second, third and fourth moment of the distribution
                var d = 1e-2;
                var p2 = Phi(process, new Complex(0, -2 * d), nu_0, nu_t, dt).Real;
                var p1 = Phi(process, new Complex(0, -d), nu_0, nu_t, dt).Real;
                var p0 = Phi(process, new Complex(0, 0), nu_0, nu_t, dt).Real;
                var pm1 = Phi(process, new Complex(0, d), nu_0, nu_t, dt).Real;
                var pm2 = Phi(process, new Complex(0, 2 * d), nu_0, nu_t, dt).Real;

                var avg = (pm2 - 8 * pm1 + 8 * p1 - p2) / (12 * d);
                var m2 = (-pm2 + 16 * pm1 - 30 * p0 + 16 * p1 - p2) / (12 * d * d);
                var var = m2 - avg * avg;
                var stdDev = System.Math.Sqrt(var);

                var m3 = (-0.5 * pm2 + pm1 - p1 + 0.5 * p2) / (d * d * d);
                var skew = (m3 - 3 * var * avg - avg * avg * avg) / (var * stdDev);

                var m4 = (pm2 - 4 * pm1 + 6 * p0 - 4 * p1 + p2) / (d * d * d * d);
                var kurt = (m4 - 4 * m3 * avg + 6 * m2 * avg * avg - 3 * avg * avg * avg * avg) / (var * var);

                // Cornish-Fisher relation to come up with an improved
                // estimate of 1-F(u_\eps) < \eps
                var q = new InverseCumulativeNormal().value(1 - eps);
                var w = q + (q * q - 1) / 6 * skew + (q * q * q - 3 * q) / 24 * (kurt - 3)
                        - (2 * q * q * q - 5 * q) / 36 * skew * skew;

                return avg + w * stdDev;
            }

            private double pade(double x, double[] nominator, double[] denominator, int m)
            {
                double n = 0.0, d = 0.0;
                for (var i = m - 1; i >= 0; --i)
                {
                    n = (n + nominator[i]) * x;
                    d = (d + denominator[i]) * x;
                }

                return (1 + n) / (1 + d);
            }

            // This is the continuous version of a characteristic function
            // for the exact sampling of the Heston process, s. page 8, formula 13,
            // M. Broadie, O. Kaya, Exact Simulation of Stochastic Volatility and
            // other Affine Jump Diffusion Processes
            // http://finmath.stanford.edu/seminars/documents/Broadie.pdf
            //
            // This version does not need a branch correction procedure.
            // For details please see:
            // Roger Lord, "Efficient Pricing Algorithms for exotic Derivatives",
            // http://repub.eur.nl/pub/13917/LordR-Thesis.pdf
            private Complex Phi(HestonProcess process, Complex a, double nu_0, double nu_t, double dt)
            {
                var theta = process.theta();
                var kappa = process.kappa();
                var sigma = process.sigma();

                var sigma2 = sigma * sigma;
                var ga = Complex.Sqrt(kappa * kappa - 2 * sigma2 * a * new Complex(0.0, 1.0));
                var d = 4 * theta * kappa / sigma2;

                var nu = 0.5 * d - 1;
                var z = ga * Complex.Exp(-0.5 * ga * dt) / (1.0 - Complex.Exp(-ga * dt));
                var log_z = -0.5 * ga * dt + Complex.Log(ga / (1.0 - Complex.Exp(-ga * dt)));

                var alpha = 4.0 * ga * Complex.Exp(-0.5 * ga * dt) / (sigma2 * (1.0 - Complex.Exp(-ga * dt)));
                var beta = 4.0 * kappa * Complex.Exp(-0.5 * kappa * dt) / (sigma2 * (1.0 - Complex.Exp(-kappa * dt)));

                return ga * Complex.Exp(-0.5 * (ga - kappa) * dt) * (1 - Complex.Exp(-kappa * dt))
                       / (kappa * (1.0 - Complex.Exp(-ga * dt)))
                       * Complex.Exp((nu_0 + nu_t) / sigma2 * (
                           kappa * (1.0 + Complex.Exp(-kappa * dt)) / (1.0 - Complex.Exp(-kappa * dt))
                           - ga * (1.0 + Complex.Exp(-ga * dt)) / (1.0 - Complex.Exp(-ga * dt))))
                       * Complex.Exp(nu * log_z) / Complex.Pow(z, nu)
                       * (nu_t > 1e-8
                           ? Math.Utils.modifiedBesselFunction_i(nu, Complex.Sqrt(nu_0 * nu_t) * alpha)
                             / Math.Utils.modifiedBesselFunction_i(nu, Complex.Sqrt(nu_0 * nu_t) * beta)
                           : Complex.Pow(alpha / beta, nu)
                       );
            }

            // For the definition of the Pade approximation please see e.g.
            // http://wikipedia.org/wiki/Sine_integral#Sine_integral
            private double Si(double x)
            {
                if (x <= 4.0)
                {
                    double[] n =
                    {
                        -4.54393409816329991e-2, 1.15457225751016682e-3,
                        -1.41018536821330254e-5, 9.43280809438713025e-8,
                        -3.53201978997168357e-10, 7.08240282274875911e-13,
                        -6.05338212010422477e-16
                    };
                    double[] d =
                    {
                        1.01162145739225565e-2, 4.99175116169755106e-5,
                        1.55654986308745614e-7, 3.28067571055789734e-10,
                        4.5049097575386581e-13, 3.21107051193712168e-16,
                        0.0
                    };

                    return x * pade(x * x, n, d, n.Length); // TODO sizeof(n)/sizeof(Real)
                }

                var y = 1 / (x * x);
                double[] fn =
                {
                    7.44437068161936700618e2, 1.96396372895146869801e5,
                    2.37750310125431834034e7, 1.43073403821274636888e9,
                    4.33736238870432522765e10, 6.40533830574022022911e11,
                    4.20968180571076940208e12, 1.00795182980368574617e13,
                    4.94816688199951963482e12, -4.94701168645415959931e11
                };
                double[] fd =
                {
                    7.46437068161927678031e2, 1.97865247031583951450e5,
                    2.41535670165126845144e7, 1.47478952192985464958e9,
                    4.58595115847765779830e10, 7.08501308149515401563e11,
                    5.06084464593475076774e12, 1.43468549171581016479e13,
                    1.11535493509914254097e13, 0.0
                };
                var f = pade(y, fn, fd, 10) / x;

                double[] gn =
                {
                    8.1359520115168615e2, 2.35239181626478200e5,
                    3.12557570795778731e7, 2.06297595146763354e9,
                    6.83052205423625007e10, 1.09049528450362786e12,
                    7.57664583257834349e12, 1.81004487464664575e13,
                    6.43291613143049485e12, -1.36517137670871689e12
                };
                double[] gd =
                {
                    8.19595201151451564e2, 2.40036752835578777e5,
                    3.26026661647090822e7, 2.23355543278099360e9,
                    7.87465017341829930e10, 1.39866710696414565e12,
                    1.17164723371736605e13, 4.01839087307656620e13,
                    3.99653257887490811e13, 0.0
                };
                var g = y * pade(y, gn, gd, 10);

                return Const.M_PI_2 - f * System.Math.Cos(x) - g * System.Math.Sin(x);
            }
        }

        private class cdf_nu_ds_minus_x : ISolver1d
        {
            private readonly cdf_nu_ds cdf_nu;
            private readonly double x0;

            public cdf_nu_ds_minus_x(double _x0, HestonProcess _process, double _nu_0, double _nu_t, double _dt,
                Discretization _discretization)
            {
                cdf_nu = new cdf_nu_ds(_process, _nu_0, _nu_t, _dt, _discretization);
                x0 = _x0;
            }

            public override double value(double v) => cdf_nu.value(v) - x0;
        }

        private class ch
        {
            private readonly double dt;
            private readonly HestonProcess process;
            private readonly double x;
            private readonly double nu_0;
            private readonly double nu_t;

            public ch(HestonProcess _process, double _x, double _nu_0, double _nu_t, double _dt)
            {
                process = _process;
                x = _x;
                nu_0 = _nu_0;
                nu_t = _nu_t;
                dt = _dt;
            }

            public double value(double u) => Const.M_2_PI * System.Math.Sin(u * x) / u * Phi(process, u, nu_0, nu_t, dt).Real;

            private Complex Phi(HestonProcess process, Complex a, double nu_0, double nu_t, double dt)
            {
                var theta = process.theta();
                var kappa = process.kappa();
                var sigma = process.sigma();

                var sigma2 = sigma * sigma;
                var ga = Complex.Sqrt(kappa * kappa - 2 * sigma2 * a * new Complex(0.0, 1.0));
                var d = 4 * theta * kappa / sigma2;

                var nu = 0.5 * d - 1;
                var z = ga * Complex.Exp(-0.5 * ga * dt) / (1.0 - Complex.Exp(-ga * dt));
                var log_z = -0.5 * ga * dt + Complex.Log(ga / (1.0 - Complex.Exp(-ga * dt)));

                var alpha = 4.0 * ga * Complex.Exp(-0.5 * ga * dt) / (sigma2 * (1.0 - Complex.Exp(-ga * dt)));
                var beta = 4.0 * kappa * Complex.Exp(-0.5 * kappa * dt) / (sigma2 * (1.0 - Complex.Exp(-kappa * dt)));

                return ga * Complex.Exp(-0.5 * (ga - kappa) * dt) * (1 - Complex.Exp(-kappa * dt))
                       / (kappa * (1.0 - Complex.Exp(-ga * dt)))
                       * Complex.Exp((nu_0 + nu_t) / sigma2 * (
                           kappa * (1.0 + Complex.Exp(-kappa * dt)) / (1.0 - Complex.Exp(-kappa * dt))
                           - ga * (1.0 + Complex.Exp(-ga * dt)) / (1.0 - Complex.Exp(-ga * dt))))
                       * Complex.Exp(nu * log_z) / Complex.Pow(z, nu)
                       * (nu_t > 1e-8
                           ? Math.Utils.modifiedBesselFunction_i(nu, Complex.Sqrt(nu_0 * nu_t) * alpha)
                             / Math.Utils.modifiedBesselFunction_i(nu, Complex.Sqrt(nu_0 * nu_t) * beta)
                           : Complex.Pow(alpha / beta, nu)
                       );
            }
        }

        private new Discretization discretization_;
        private Handle<YieldTermStructure> riskFreeRate_, dividendYield_;
        private Handle<Quote> s0_;
        private double v0_, kappa_, theta_, sigma_, rho_;

        public HestonProcess(Handle<YieldTermStructure> riskFreeRate,
            Handle<YieldTermStructure> dividendYield,
            Handle<Quote> s0,
            double v0, double kappa,
            double theta, double sigma, double rho,
            Discretization d = Discretization.QuadraticExponentialMartingale)
            : base(new EulerDiscretization())
        {
            riskFreeRate_ = riskFreeRate;
            dividendYield_ = dividendYield;
            s0_ = s0;
            v0_ = v0;
            kappa_ = kappa;
            theta_ = theta;
            sigma_ = sigma;
            rho_ = rho;
            discretization_ = d;

            riskFreeRate_.registerWith(update);
            dividendYield_.registerWith(update);
            s0_.registerWith(update);
        }

        public override Vector apply(Vector x0, Vector dx)
        {
            var tmp = new Vector(2);
            tmp[0] = x0[0] * System.Math.Exp(dx[0]);
            tmp[1] = x0[1] + dx[1];
            return tmp;
        }

        public override Matrix diffusion(double t, Vector x)
        {
            /* the correlation matrix is
              |  1   rho |
              | rho   1  |
              whose square root (which is used here) is
              |  1          0       |
              | rho   sqrt(1-rho^2) |
            */
            var tmp = new Matrix(2, 2);
            var vol = x[1] > 0.0 ? System.Math.Sqrt(x[1])
                : discretization_ == Discretization.Reflection ? -System.Math.Sqrt(-x[1])
                : 1e-8; // set vol to (almost) zero but still
            // expose some correlation information
            var sigma2 = sigma_ * vol;
            var sqrhov = System.Math.Sqrt(1.0 - rho_ * rho_);

            tmp[0, 0] = vol;
            tmp[0, 1] = 0.0;
            tmp[1, 0] = rho_ * sigma2;
            tmp[1, 1] = sqrhov * sigma2;
            return tmp;
        }

        public Handle<YieldTermStructure> dividendYield() => dividendYield_;

        public override Vector drift(double t, Vector x)
        {
            var tmp = new Vector(2);
            var vol = x[1] > 0.0 ? System.Math.Sqrt(x[1])
                : discretization_ == Discretization.Reflection ? -System.Math.Sqrt(-x[1])
                : 0.0;

            tmp[0] = riskFreeRate_.link.forwardRate(t, t, Compounding.Continuous).value()
                     - dividendYield_.link.forwardRate(t, t, Compounding.Continuous).value()
                     - 0.5 * vol * vol;

            tmp[1] = kappa_ * (theta_ - (discretization_ == Discretization.PartialTruncation ? x[1] : vol * vol));
            return tmp;
        }

        public override Vector evolve(double t0, Vector x0, double dt, Vector dw)
        {
            var retVal = new Vector(2);
            double vol, vol2, mu, nu, dy;

            var sdt = System.Math.Sqrt(dt);
            var sqrhov = System.Math.Sqrt(1.0 - rho_ * rho_);

            switch (discretization_)
            {
                // For the definition of PartialTruncation, FullTruncation
                // and Reflection  see Lord, R., R. Koekkoek and D. van Dijk (2006),
                // "A Comparison of biased simulation schemes for
                //  stochastic volatility models",
                // Working Paper, Tinbergen Institute
                case Discretization.PartialTruncation:
                    vol = x0[1] > 0.0 ? System.Math.Sqrt(x0[1]) : 0.0;
                    vol2 = sigma_ * vol;
                    mu = riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                         - dividendYield_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                         - 0.5 * vol * vol;
                    nu = kappa_ * (theta_ - x0[1]);

                    retVal[0] = x0[0] * System.Math.Exp(mu * dt + vol * dw[0] * sdt);
                    retVal[1] = x0[1] + nu * dt + vol2 * sdt * (rho_ * dw[0] + sqrhov * dw[1]);
                    break;
                case Discretization.FullTruncation:
                    vol = x0[1] > 0.0 ? System.Math.Sqrt(x0[1]) : 0.0;
                    vol2 = sigma_ * vol;
                    mu = riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                         - dividendYield_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                         - 0.5 * vol * vol;
                    nu = kappa_ * (theta_ - vol * vol);

                    retVal[0] = x0[0] * System.Math.Exp(mu * dt + vol * dw[0] * sdt);
                    retVal[1] = x0[1] + nu * dt + vol2 * sdt * (rho_ * dw[0] + sqrhov * dw[1]);
                    break;
                case Discretization.Reflection:
                    vol = System.Math.Sqrt(System.Math.Abs(x0[1]));
                    vol2 = sigma_ * vol;
                    mu = riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                         - dividendYield_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                         - 0.5 * vol * vol;
                    nu = kappa_ * (theta_ - vol * vol);

                    retVal[0] = x0[0] * System.Math.Exp(mu * dt + vol * dw[0] * sdt);
                    retVal[1] = vol * vol
                                + nu * dt + vol2 * sdt * (rho_ * dw[0] + sqrhov * dw[1]);
                    break;
                case Discretization.NonCentralChiSquareVariance:
                    // use Alan Lewis trick to decorrelate the equity and the variance
                    // process by using y(t)=x(t)-\frac{rho}{sigma}\nu(t)
                    // and Ito's Lemma. Then use exact sampling for the variance
                    // process. For further details please read the Wilmott thread
                    // "QuantLib code is very high quality"
                    vol = x0[1] > 0.0 ? System.Math.Sqrt(x0[1]) : 0.0;
                    mu = riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                         - dividendYield_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                         - 0.5 * vol * vol;

                    retVal[1] = varianceDistribution(x0[1], dw[1], dt);
                    dy = (mu - rho_ / sigma_ * kappa_
                                             * (theta_ - vol * vol)) * dt + vol * sqrhov * dw[0] * sdt;

                    retVal[0] = x0[0] * System.Math.Exp(dy + rho_ / sigma_ * (retVal[1] - x0[1]));
                    break;
                case Discretization.QuadraticExponential:
                case Discretization.QuadraticExponentialMartingale:
                {
                    // for details of the quadratic exponential discretization scheme
                    // see Leif Andersen,
                    // Efficient Simulation of the Heston Stochastic Volatility Model
                    var ex = System.Math.Exp(-kappa_ * dt);

                    var m = theta_ + (x0[1] - theta_) * ex;
                    var s2 = x0[1] * sigma_ * sigma_ * ex / kappa_ * (1 - ex)
                             + theta_ * sigma_ * sigma_ / (2 * kappa_) * (1 - ex) * (1 - ex);
                    var psi = s2 / (m * m);

                    var g1 = 0.5;
                    var g2 = 0.5;
                    var k0 = -rho_ * kappa_ * theta_ * dt / sigma_;
                    var k1 = g1 * dt * (kappa_ * rho_ / sigma_ - 0.5) - rho_ / sigma_;
                    var k2 = g2 * dt * (kappa_ * rho_ / sigma_ - 0.5) + rho_ / sigma_;
                    var k3 = g1 * dt * (1 - rho_ * rho_);
                    var k4 = g2 * dt * (1 - rho_ * rho_);
                    var A = k2 + 0.5 * k4;

                    if (psi < 1.5)
                    {
                        var b2 = 2 / psi - 1 + System.Math.Sqrt(2 / psi * (2 / psi - 1));
                        var b = System.Math.Sqrt(b2);
                        var a = m / (1 + b2);

                        if (discretization_ == Discretization.QuadraticExponentialMartingale)
                        {
                            // martingale correction
                            QLNet.Utils.QL_REQUIRE(A < 1 / (2 * a), () => "illegal value");
                            k0 = -A * b2 * a / (1 - 2 * A * a) + 0.5 * System.Math.Log(1 - 2 * A * a)
                                 - (k1 + 0.5 * k3) * x0[1];
                        }

                        retVal[1] = a * (b + dw[1]) * (b + dw[1]);
                    }
                    else
                    {
                        var p = (psi - 1) / (psi + 1);
                        var beta = (1 - p) / m;

                        var u = new CumulativeNormalDistribution().value(dw[1]);

                        if (discretization_ == Discretization.QuadraticExponentialMartingale)
                        {
                            // martingale correction
                            QLNet.Utils.QL_REQUIRE(A < beta, () => "illegal value");
                            k0 = -System.Math.Log(p + beta * (1 - p) / (beta - A)) - (k1 + 0.5 * k3) * x0[1];
                        }

                        retVal[1] = u <= p ? 0.0 : System.Math.Log((1 - p) / (1 - u)) / beta;
                    }

                    mu = riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                         - dividendYield_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value();

                    retVal[0] = x0[0] * System.Math.Exp(mu * dt + k0 + k1 * x0[1] + k2 * retVal[1]
                                                        + System.Math.Sqrt(k3 * x0[1] + k4 * retVal[1]) * dw[0]);
                }
                    break;
                case Discretization.BroadieKayaExactSchemeLobatto:
                case Discretization.BroadieKayaExactSchemeLaguerre:
                case Discretization.BroadieKayaExactSchemeTrapezoidal:
                {
                    var nu_0 = x0[1];
                    var nu_t = varianceDistribution(nu_0, dw[1], dt);

                    var x = System.Math.Min(1.0 - Const.QL_EPSILON,
                        System.Math.Max(0.0, new CumulativeNormalDistribution().value(dw[2])));

                    var f = new cdf_nu_ds_minus_x(x, this, nu_0, nu_t, dt, discretization_);
                    var vds = new Brent().solve(f, 1e-5, theta_ * dt, 0.1 * theta_ * dt);

                    var vdw = (nu_t - nu_0 - kappa_ * theta_ * dt + kappa_ * vds) / sigma_;

                    mu = (riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()
                          - dividendYield_.link.forwardRate(t0, t0 + dt, Compounding.Continuous).value()) * dt
                        - 0.5 * vds + rho_ * vdw;

                    var sig = System.Math.Sqrt((1 - rho_ * rho_) * vds);
                    var s = x0[0] * System.Math.Exp(mu + sig * dw[0]);

                    retVal[0] = s;
                    retVal[1] = nu_t;
                }
                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown discretization schema");
                    break;
            }

            return retVal;
        }

        public override int factors() =>
            discretization_ == Discretization.BroadieKayaExactSchemeLobatto
            || discretization_ == Discretization.BroadieKayaExactSchemeTrapezoidal
            || discretization_ == Discretization.BroadieKayaExactSchemeLaguerre
                ? 3
                : 2;

        public override Vector initialValues()
        {
            var tmp = new Vector(2);
            tmp[0] = s0_.link.value();
            tmp[1] = v0_;
            return tmp;
        }

        public double kappa() => kappa_;

        public double rho() => rho_;

        public Handle<YieldTermStructure> riskFreeRate() => riskFreeRate_;

        public Handle<Quote> s0() => s0_;

        public double sigma() => sigma_;

        public override int size() => 2;

        public double theta() => theta_;

        public override double time(Date d) => riskFreeRate_.link.dayCounter().yearFraction(riskFreeRate_.link.referenceDate(), d);

        public double v0() => v0_;

        private double varianceDistribution(double v, double dw, double dt)
        {
            var df = 4 * theta_ * kappa_ / (sigma_ * sigma_);
            var ncp = 4 * kappa_ * System.Math.Exp(-kappa_ * dt)
                / (sigma_ * sigma_ * (1 - System.Math.Exp(-kappa_ * dt))) * v;

            var p = System.Math.Min(1.0 - Const.QL_EPSILON,
                System.Math.Max(0.0, new CumulativeNormalDistribution().value(dw)));

            return sigma_ * sigma_ * (1 - System.Math.Exp(-kappa_ * dt)) / (4 * kappa_)
                   * new InverseNonCentralChiSquareDistribution(df, ncp, 100).value(p);
        }
    }
}
