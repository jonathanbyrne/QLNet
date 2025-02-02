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
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments
{
    //! cliquet (Ratchet) option
    /*! A cliquet option, also known as Ratchet option, is a series of
        forward-starting (a.k.a. deferred strike) options where the
        strike for each forward start option is set equal to a fixed
        percentage of the spot price at the beginning of each period.

        \todo
        - add local/global caps/floors
        - add accrued coupon and last fixing

        \ingroup instruments
    */
    [PublicAPI]
    public class CliquetOption : OneAssetOption
    {
        //! %Arguments for cliquet option calculation
        // should inherit from a strikeless version of VanillaOption::arguments
        public new class Arguments : Option.Arguments
        {
            public Arguments()
            {
                accruedCoupon = null;
                lastFixing = null;
                localCap = null;
                localFloor = null;
                globalCap = null;
                globalFloor = null;
            }

            public double? accruedCoupon { get; set; }

            public double? globalCap { get; set; }

            public double? globalFloor { get; set; }

            public double? lastFixing { get; set; }

            public double? localCap { get; set; }

            public double? localFloor { get; set; }

            public List<Date> resetDates { get; set; }

            public override void validate()
            {
                var moneyness = payoff as PercentageStrikePayoff;
                QLNet.Utils.QL_REQUIRE(moneyness != null, () => "wrong payoff ExerciseType");
                QLNet.Utils.QL_REQUIRE(moneyness.strike() > 0.0, () => "negative or zero moneyness given");
                QLNet.Utils.QL_REQUIRE(accruedCoupon == null || accruedCoupon >= 0.0, () => "negative accrued coupon");
                QLNet.Utils.QL_REQUIRE(localCap == null || localCap >= 0.0, () => "negative local cap");
                QLNet.Utils.QL_REQUIRE(localFloor == null || localFloor >= 0.0, () => "negative local floor");
                QLNet.Utils.QL_REQUIRE(globalCap == null || globalCap >= 0.0, () => "negative global cap");
                QLNet.Utils.QL_REQUIRE(globalFloor == null || globalFloor >= 0.0, () => "negative global floor");
                QLNet.Utils.QL_REQUIRE(!resetDates.empty(), () => "no reset dates given");
                for (var i = 0; i < resetDates.Count; ++i)
                {
                    QLNet.Utils.QL_REQUIRE(exercise.lastDate() > resetDates[i], () => "reset date greater or equal to maturity");
                    QLNet.Utils.QL_REQUIRE(i == 0 || resetDates[i] > resetDates[i - 1], () => "unsorted reset dates");
                }
            }
        }

        //! Cliquet %engine base class
        public new class Engine : GenericEngine<Arguments, Results>
        {
        }

        private List<Date> resetDates_;

        public CliquetOption(PercentageStrikePayoff payoff, EuropeanExercise maturity, List<Date> resetDates)
            : base(payoff, maturity)
        {
            resetDates_ = new List<Date>(resetDates);
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);
            // set accrued coupon, last fixing, caps, floors
            var moreArgs = args as Arguments;
            QLNet.Utils.QL_REQUIRE(moreArgs != null, () => "wrong engine ExerciseType");
            moreArgs.resetDates = new List<Date>(resetDates_);
        }
    }
}
