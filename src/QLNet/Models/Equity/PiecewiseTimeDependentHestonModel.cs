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
using QLNet.Math.Optimization;
using QLNet.Quotes;
using QLNet.Termstructures;

namespace QLNet.Models.Equity
{
    //! Piecewise time dependent Heston model
    /*! References:

        Heston, Steven L., 1993. A Closed-Form Solution for Options
        with Stochastic Volatility with Applications to Bond and
        Currency Options.  The review of Financial Studies, Volume 6,
        Issue 2, 327-343.

        A. Elices, Models with time-dependent parameters using
        transform methods: application to Heston’s model,
        http://arxiv.org/pdf/0708.2020
    */
    [PublicAPI]
    public class PiecewiseTimeDependentHestonModel : CalibratedModel
    {
        protected Handle<YieldTermStructure> dividendYield_;
        protected Handle<YieldTermStructure> riskFreeRate_;
        protected Handle<Quote> s0_;
        protected TimeGrid timeGrid_;

        public PiecewiseTimeDependentHestonModel(Handle<YieldTermStructure> riskFreeRate,
            Handle<YieldTermStructure> dividendYield,
            Handle<Quote> s0,
            double v0,
            Parameter theta,
            Parameter kappa,
            Parameter sigma,
            Parameter rho,
            TimeGrid timeGrid)
            : base(5)
        {
            s0_ = s0;
            riskFreeRate_ = riskFreeRate;
            dividendYield_ = dividendYield;
            timeGrid_ = timeGrid;

            arguments_[0] = theta;
            arguments_[1] = kappa;
            arguments_[2] = sigma;
            arguments_[3] = rho;
            arguments_[4] = new ConstantParameter(v0, new PositiveConstraint());

            s0.registerWith(update);
            riskFreeRate.registerWith(update);
            dividendYield.registerWith(update);
        }

        public Handle<YieldTermStructure> dividendYield() => dividendYield_;

        // variance mean reversion speed
        public double kappa(double t) => arguments_[1].value(t);

        // correlation
        public double rho(double t) => arguments_[3].value(t);

        public Handle<YieldTermStructure> riskFreeRate() => riskFreeRate_;

        // spot
        public double s0() => s0_.link.value();

        // volatility of the volatility
        public double sigma(double t) => arguments_[2].value(t);

        // variance mean version level
        public double theta(double t) => arguments_[0].value(t);

        public TimeGrid timeGrid() => timeGrid_;

        // spot variance
        public double v0() => arguments_[4].value(0.0);
    }
}
