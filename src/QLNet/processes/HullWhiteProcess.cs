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

using JetBrains.Annotations;
using QLNet.Termstructures;

namespace QLNet.Processes
{
    //! Hull-White stochastic process
    [PublicAPI]
    public class HullWhiteProcess : StochasticProcess1D
    {
        protected double a_, sigma_;
        protected Handle<YieldTermStructure> h_;
        protected OrnsteinUhlenbeckProcess process_;

        public HullWhiteProcess(Handle<YieldTermStructure> h, double a, double sigma)
        {
            process_ = new OrnsteinUhlenbeckProcess(a, sigma, h.link.forwardRate(0.0, 0.0, Compounding.Continuous, Frequency.NoFrequency).value());
            h_ = h;
            a_ = a;
            sigma_ = sigma;

            QLNet.Utils.QL_REQUIRE(a_ >= 0.0, () => "negative a given");
            QLNet.Utils.QL_REQUIRE(sigma_ >= 0.0, () => "negative sigma given");
        }

        public double a() => a_;

        public double alpha(double t)
        {
            var alfa = a_ > Const.QL_EPSILON ? sigma_ / a_ * (1 - System.Math.Exp(-a_ * t)) : sigma_ * t;
            alfa *= 0.5 * alfa;
            alfa += h_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).value();
            return alfa;
        }

        public override double diffusion(double t, double x) => process_.diffusion(t, x);

        public override double drift(double t, double x)
        {
            var alpha_drift = sigma_ * sigma_ / (2 * a_) * (1 - System.Math.Exp(-2 * a_ * t));
            var shift = 0.0001;
            var f = h_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).value();
            var fup = h_.link.forwardRate(t + shift, t + shift, Compounding.Continuous, Frequency.NoFrequency).value();
            var f_prime = (fup - f) / shift;
            alpha_drift += a_ * f + f_prime;
            return process_.drift(t, x) + alpha_drift;
        }

        public override double expectation(double t0, double x0, double dt) =>
            process_.expectation(t0, x0, dt)
            + alpha(t0 + dt) - alpha(t0) * System.Math.Exp(-a_ * dt);

        public double sigma() => sigma_;

        public override double stdDeviation(double t0, double x0, double dt) => process_.stdDeviation(t0, x0, dt);

        public override double variance(double t0, double x0, double dt) => process_.variance(t0, x0, dt);

        // StochasticProcess1D interface
        public override double x0() => process_.x0();
    }

    //! %Forward Hull-White stochastic process
    /*! \ingroup processes */
}
