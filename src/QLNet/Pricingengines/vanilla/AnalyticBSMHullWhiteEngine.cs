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
using QLNet.Instruments;
using QLNet.Models.Shortrate.Onefactormodels;
using QLNet.Processes;
using QLNet.Termstructures.Volatility.equityfx;

namespace QLNet.PricingEngines.vanilla
{
    //! analytic european option pricer including stochastic interest rates
    /*! References:

        Brigo, Mercurio, Interest Rate Models

        \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in web/literature
    */
    [PublicAPI]
    public class AnalyticBSMHullWhiteEngine : GenericModelEngine<HullWhite, QLNet.Option.Arguments,
        OneAssetOption.Results>
    {
        private GeneralizedBlackScholesProcess process_;
        private double rho_;

        public AnalyticBSMHullWhiteEngine(double equityShortRateCorrelation,
            GeneralizedBlackScholesProcess process,
            HullWhite model)
            : base(model)
        {
            rho_ = equityShortRateCorrelation;
            process_ = process;

            process_.registerWith(update);
        }

        public override void calculate()
        {
            QLNet.Utils.QL_REQUIRE(process_.x0() > 0.0, () => "negative or null underlying given");

            var payoff = arguments_.payoff as StrikedTypePayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-striked payoff given");

            var exercise = arguments_.exercise;

            var t = process_.riskFreeRate().link.dayCounter().yearFraction(process_.riskFreeRate().link.referenceDate(),
                exercise.lastDate());

            var a = model_.link.parameters()[0];
            var sigma = model_.link.parameters()[1];
            var eta = process_.blackVolatility().link.blackVol(exercise.lastDate(), payoff.strike());

            double varianceOffset;
            if (a * t > System.Math.Pow(Const.QL_EPSILON, 0.25))
            {
                var v = sigma * sigma / (a * a) * (t + 2 / a * System.Math.Exp(-a * t) - 1 / (2 * a) * System.Math.Exp(-2 * a * t) - 3 / (2 * a));
                var mu = 2 * rho_ * sigma * eta / a * (t - 1 / a * (1 - System.Math.Exp(-a * t)));

                varianceOffset = v + mu;
            }
            else
            {
                // low-a algebraic limit
                var v = sigma * sigma * t * t * t * (1 / 3.0 - 0.25 * a * t + 7 / 60.0 * a * a * t * t);
                var mu = rho_ * sigma * eta * t * t * (1 - a * t / 3.0 + a * a * t * t / 12.0);

                varianceOffset = v + mu;
            }

            var volTS = new Handle<BlackVolTermStructure>(
                new ShiftedBlackVolTermStructure(varianceOffset, process_.blackVolatility()));

            var adjProcess =
                new GeneralizedBlackScholesProcess(process_.stateVariable(),
                    process_.dividendYield(),
                    process_.riskFreeRate(),
                    volTS);

            var bsmEngine = new AnalyticEuropeanEngine(adjProcess);

            var option = new VanillaOption(payoff, exercise);
            option.setupArguments(bsmEngine.getArguments());

            bsmEngine.calculate();

            results_ = bsmEngine.getResults() as OneAssetOption.Results;
        }
    }
}
