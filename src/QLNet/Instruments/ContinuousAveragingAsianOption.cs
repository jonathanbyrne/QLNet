/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Instruments
{

    //! Continuous-averaging Asian option
    //    ! \todo add running average
    //
    //        \ingroup instruments
    //
    [JetBrains.Annotations.PublicAPI] public class ContinuousAveragingAsianOption : OneAssetOption
    {
        public new class Arguments : Option.Arguments
        {
            public Arguments()
            {
                averageType = Average.Type.NULL;
            }
            public override void validate()
            {
                base.validate();
                Utils.QL_REQUIRE(averageType != Average.Type.NULL, () => "unspecified average ExerciseType");
            }
            public Average.Type averageType { get; set; }
        }

        public new class Engine : GenericEngine<Arguments, Results>
        {
        }

        public ContinuousAveragingAsianOption(Average.Type averageType, StrikedTypePayoff payoff, Exercise exercise) : base(payoff, exercise)
        {
            averageType_ = averageType;
        }
        public override void setupArguments(IPricingEngineArguments args)
        {

            base.setupArguments(args);

            var moreArgs = args as Arguments;
            Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument ExerciseType");
            moreArgs.averageType = averageType_;
        }
        protected Average.Type averageType_;
    }

    //! Discrete-averaging Asian option
    //! \ingroup instruments
}
