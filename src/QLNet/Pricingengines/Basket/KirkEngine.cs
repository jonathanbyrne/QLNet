﻿//
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
//

using System;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Processes;

namespace QLNet.PricingEngines.Basket
{
    //! Pricing engine for spread option on two futures
    /*! This class implements formulae from
        "Correlation in the Energy Markets", E. Kirk
        Managing Energy Price Risk.
        London: Risk Publications and Enron, pp. 71-78

        \ingroup basketengines

        \test the correctness of the returned value is tested by
              reproducing results available in literature.
    */
    [PublicAPI]
    public class KirkEngine : BasketOption.Engine
    {
        private BlackProcess process1_;
        private BlackProcess process2_;
        private double rho_;

        public KirkEngine(BlackProcess process1,
            BlackProcess process2,
            double correlation)
        {
            process1_ = process1;
            process2_ = process2;
            rho_ = correlation;

            process1_.registerWith(update);
            process2_.registerWith(update);
        }

        public override void calculate()
        {
            QLNet.Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () => "not an European Option");

            var exercise = arguments_.exercise as EuropeanExercise;
            QLNet.Utils.QL_REQUIRE(exercise != null, () => "not an European Option");

            var spreadPayoff = arguments_.payoff as SpreadBasketPayoff;
            QLNet.Utils.QL_REQUIRE(spreadPayoff != null, () => " spread payoff expected");

            var payoff = spreadPayoff.basePayoff() as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");
            var strike = payoff.strike();

            var f1 = process1_.stateVariable().link.value();
            var f2 = process2_.stateVariable().link.value();

            // use atm vols
            var variance1 = process1_.blackVolatility().link.blackVariance(exercise.lastDate(), f1);
            var variance2 = process2_.blackVolatility().link.blackVariance(exercise.lastDate(), f2);

            var riskFreeDiscount = process1_.riskFreeRate().link.discount(exercise.lastDate());

            Func<double, double> Square = x => x * x;
            var f = f1 / (f2 + strike);
            var v = System.Math.Sqrt(variance1
                                     + variance2 * Square(f2 / (f2 + strike))
                                     - 2 * rho_ * System.Math.Sqrt(variance1 * variance2)
                                     * (f2 / (f2 + strike)));

            var black = new BlackCalculator(new PlainVanillaPayoff(payoff.optionType(), 1.0), f, v, riskFreeDiscount);

            results_.value = (f2 + strike) * black.value();
        }
    }
}
