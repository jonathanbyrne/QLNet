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

using JetBrains.Annotations;

namespace QLNet.Instruments
{
    //! Continuous-floating lookback option
    [PublicAPI]
    public class ContinuousFloatingLookbackOption : OneAssetOption
    {
        //! %Arguments for continuous fixed lookback option calculation
        public new class Arguments : Option.Arguments
        {
            public double? minmax { get; set; }

            public override void validate()
            {
                base.validate();

                QLNet.Utils.QL_REQUIRE(minmax != null, () => "null prior extremum");
                QLNet.Utils.QL_REQUIRE(minmax >= 0.0, () => "nonnegative prior extremum required: " + minmax + " not allowed");
            }
        }

        //! %Continuous floating lookback %engine base class
        public new class Engine : GenericEngine<Arguments,
            Results>
        {
        }

        // arguments
        protected double? minmax_;

        public ContinuousFloatingLookbackOption(double minmax, TypePayoff payoff, Exercise exercise)
            : base(payoff, exercise)
        {
            minmax_ = minmax;
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            var moreArgs = args as Arguments;
            QLNet.Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument ExerciseType");
            moreArgs.minmax = minmax_;
        }
    }

    //! Continuous-fixed lookback option

    //! Continuous-partial-floating lookback option
    /*! From http://help.rmetrics.org/fExoticOptions/LookbackOptions.html :

       For a partial-time floating strike lookback option, the
       lookback period starts at time zero and ends at an arbitrary
       date before expiration. Except for the partial lookback
       period, the option is similar to a floating strike lookback
       option. The partial-time floating strike lookback option is
       cheaper than a similar standard floating strike lookback
       option. Partial-time floating strike lookback options can be
       priced analytically using a model introduced by Heynen and Kat
       (1994).

    */

    //! Continuous-partial-fixed lookback option
    /*! From http://help.rmetrics.org/fExoticOptions/LookbackOptions.html :

       For a partial-time fixed strike lookback option, the lookback
       period starts at a predetermined date after the initialization
       date of the option.  The partial-time fixed strike lookback
       call option payoff is given by the difference between the
       maximum observed price of the underlying asset during the
       lookback period and the fixed strike price. The partial-time
       fixed strike lookback put option payoff is given by the
       difference between the fixed strike price and the minimum
       observed price of the underlying asset during the lookback
       period. The partial-time fixed strike lookback option is
       cheaper than a similar standard fixed strike lookback
       option. Partial-time fixed strike lookback options can be
       priced analytically using a model introduced by Heynen and Kat
       (1994).

    */
}
