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
using QLNet.Time;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class ForwardVanillaOption : OneAssetOption
    {
        public new class Arguments : Option.Arguments
        {
            public double moneyness { get; set; }

            public Date resetDate { get; set; }

            public override void validate()
            {
                QLNet.Utils.QL_REQUIRE(moneyness > 0.0, () => "negative or zero moneyness given");
                QLNet.Utils.QL_REQUIRE(resetDate != null, () => "null reset date given");
                QLNet.Utils.QL_REQUIRE(resetDate >= Settings.evaluationDate(), () => "reset date in the past");
                QLNet.Utils.QL_REQUIRE(exercise.lastDate() > resetDate, () => "reset date later or equal to maturity");
            }
        }

        // arguments
        private double moneyness_;
        private Date resetDate_;

        public ForwardVanillaOption(double moneyness,
            Date resetDate,
            StrikedTypePayoff payoff,
            Exercise exercise)
            : base(payoff, exercise)
        {
            moneyness_ = moneyness;
            resetDate_ = resetDate;
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            base.fetchResults(r);
            var results = r as Results;
            QLNet.Utils.QL_REQUIRE(results != null, () => "no results returned from pricing engine");
            delta_ = results.delta;
            gamma_ = results.gamma;
            theta_ = results.theta;
            vega_ = results.vega;
            rho_ = results.rho;
            dividendRho_ = results.dividendRho;
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);
            var arguments = args as Arguments;
            QLNet.Utils.QL_REQUIRE(arguments != null, () => "wrong argument ExerciseType");

            arguments.moneyness = moneyness_;
            arguments.resetDate = resetDate_;
        }
    }
}
