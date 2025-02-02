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
using QLNet.PricingEngines.barrier;
using QLNet.Processes;
using QLNet.Quotes;

namespace QLNet.Instruments
{
    //! %Double Barrier option on a single asset.
    /*! The analytic pricing engine will be used if none if passed.

        \ingroup instruments
    */
    [PublicAPI]
    public class DoubleBarrierOption : OneAssetOption
    {
        //! %Arguments for double barrier option calculation
        public new class Arguments : Option.Arguments
        {
            public Arguments()
            {
                barrier_lo = null;
                barrier_hi = null;
                rebate = null;
            }

            public double? barrier_hi { get; set; }

            public double? barrier_lo { get; set; }

            public DoubleBarrier.Type barrierType { get; set; }

            public double? rebate { get; set; }

            public override void validate()
            {
                base.validate();

                QLNet.Utils.QL_REQUIRE(barrierType == DoubleBarrier.Type.KnockIn ||
                                                barrierType == DoubleBarrier.Type.KnockOut ||
                                                barrierType == DoubleBarrier.Type.KIKO ||
                                                barrierType == DoubleBarrier.Type.KOKI, () =>
                    "Invalid barrier ExerciseType");

                QLNet.Utils.QL_REQUIRE(barrier_lo != null, () => "no low barrier given");
                QLNet.Utils.QL_REQUIRE(barrier_hi != null, () => "no high barrier given");
                QLNet.Utils.QL_REQUIRE(rebate != null, () => "no rebate given");
            }
        }

        //! %Double-Barrier-option %engine base class
        public new class Engine : GenericEngine<Arguments, Results>
        {
            protected bool triggered(double underlying) => underlying <= arguments_.barrier_lo || underlying >= arguments_.barrier_hi;
        }

        protected double barrier_hi_;
        protected double barrier_lo_;

        // arguments
        protected DoubleBarrier.Type barrierType_;
        protected double rebate_;

        public DoubleBarrierOption(DoubleBarrier.Type barrierType,
            double barrier_lo,
            double barrier_hi,
            double rebate,
            StrikedTypePayoff payoff,
            Exercise exercise)
            : base(payoff, exercise)
        {
            barrierType_ = barrierType;
            barrier_lo_ = barrier_lo;
            barrier_hi_ = barrier_hi;
            rebate_ = rebate;
        }

        /*! \warning see VanillaOption for notes on implied-volatility
                    calculation.
        */
        public double impliedVolatility(double targetValue,
            GeneralizedBlackScholesProcess process,
            double accuracy = 1.0e-4,
            int maxEvaluations = 100,
            double minVol = 1.0e-7,
            double maxVol = 4.0)
        {
            QLNet.Utils.QL_REQUIRE(!isExpired(), () => "option expired");

            var volQuote = new SimpleQuote();

            var newProcess = ImpliedVolatilityHelper.clone(process, volQuote);

            // engines are built-in for the time being
            IPricingEngine engine = null;

            switch (exercise_.ExerciseType())
            {
                case Exercise.Type.European:
                    engine = new AnalyticDoubleBarrierEngine(newProcess);
                    break;
                case Exercise.Type.American:
                case Exercise.Type.Bermudan:
                    QLNet.Utils.QL_FAIL("engine not available for non-European barrier option");
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
            moreArgs.barrier_lo = barrier_lo_;
            moreArgs.barrier_hi = barrier_hi_;
            moreArgs.rebate = rebate_;
        }
    }
}
