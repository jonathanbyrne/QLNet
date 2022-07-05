/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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
using QLNet.PricingEngines.barrier;
using QLNet.Processes;
using QLNet.Quotes;

namespace QLNet.Instruments
{
    //! %Barrier option on a single asset.
    //    ! The analytic pricing Engine will be used if none if passed.
    //
    //        \ingroup instruments
    //
    [PublicAPI]
    public class BarrierOption : OneAssetOption
    {
        public new class Arguments : Option.Arguments
        {
            public Arguments()
            {
                barrierType = Barrier.Type.NULL;
                barrier = null;
                rebate = null;
            }

            public double? barrier { get; set; }

            public Barrier.Type barrierType { get; set; }

            public double? rebate { get; set; }

            public override void validate()
            {
                base.validate();

                switch (barrierType)
                {
                    case Barrier.Type.DownIn:
                    case Barrier.Type.UpIn:
                    case Barrier.Type.DownOut:
                    case Barrier.Type.UpOut:
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unknown ExerciseType");
                        break;
                }

                QLNet.Utils.QL_REQUIRE(barrier != null, () => "no barrier given");
                QLNet.Utils.QL_REQUIRE(rebate != null, () => "no rebate given");
            }
        }

        public new class Engine : GenericEngine<Arguments, Results>
        {
            protected bool triggered(double underlying)
            {
                switch (arguments_.barrierType)
                {
                    case Barrier.Type.DownIn:
                    case Barrier.Type.DownOut:
                        return underlying < arguments_.barrier;
                    case Barrier.Type.UpIn:
                    case Barrier.Type.UpOut:
                        return underlying > arguments_.barrier;
                    default:
                        QLNet.Utils.QL_FAIL("unknown ExerciseType");
                        return false;
                }
            }
        }

        protected double? barrier_;

        // Arguments
        protected Barrier.Type barrierType_;
        protected double? rebate_;

        public BarrierOption(Barrier.Type barrierType, double barrier, double rebate, StrikedTypePayoff payoff, Exercise exercise) : base(payoff, exercise)
        {
            barrierType_ = barrierType;
            barrier_ = barrier;
            rebate_ = rebate;
        }

        //        ! \warning see VanillaOption for notes on implied-volatility
        //                     calculation.
        //
        public double impliedVolatility(double targetValue, GeneralizedBlackScholesProcess process, double accuracy = 1.0e-4,
            int maxEvaluations = 100, double minVol = 1.0e-7, double maxVol = 4.0)
        {
            QLNet.Utils.QL_REQUIRE(!isExpired(), () => "option expired");

            var volQuote = new SimpleQuote();

            var newProcess = ImpliedVolatilityHelper.clone(process, volQuote);

            // engines are built-in for the time being
            IPricingEngine engine = null;
            switch (exercise_.ExerciseType())
            {
                case Exercise.Type.European:
                    engine = new AnalyticBarrierEngine(newProcess);
                    break;
                case Exercise.Type.American:
                case Exercise.Type.Bermudan:
                    QLNet.Utils.QL_FAIL("Engine not available for non-European barrier option");
                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown exercise ExerciseType");
                    break;
            }

            return ImpliedVolatilityHelper.calculate(this, engine, volQuote, targetValue, accuracy, maxEvaluations, minVol, maxVol);
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            var moreArgs = args as Arguments;
            QLNet.Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument ExerciseType");

            moreArgs.barrierType = barrierType_;
            moreArgs.barrier = barrier_;
            moreArgs.rebate = rebate_;
        }
    }
}
