/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using JetBrains.Annotations;
using QLNet.Instruments;

namespace QLNet
{
    //! base option class
    [PublicAPI]
    public class Option : Instrument
    {
        public enum Type
        {
            Put = -1,
            Call = 1
        }

        //! basic %option %arguments
        [PublicAPI]
        public class Arguments : IPricingEngineArguments
        {
            public Exercise exercise { get; set; }

            public Payoff payoff { get; set; }

            public virtual void validate()
            {
                Utils.QL_REQUIRE(payoff != null, () => "no payoff given");
                Utils.QL_REQUIRE(exercise != null, () => "no exercise given");
            }
        }

        protected Exercise exercise_;

        // arguments
        protected Payoff payoff_;

        public Option(Payoff payoff, Exercise exercise)
        {
            payoff_ = payoff;
            exercise_ = exercise;
        }

        public Exercise exercise() => exercise_;

        public Payoff payoff() => payoff_;

        public override void setupArguments(IPricingEngineArguments args)
        {
            var arguments = args as Arguments;

            Utils.QL_REQUIRE(arguments != null, () => "wrong argument ExerciseType");

            arguments.payoff = payoff_;
            arguments.exercise = exercise_;
        }
    }
}
