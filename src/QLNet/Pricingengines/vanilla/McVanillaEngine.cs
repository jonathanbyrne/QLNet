﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using QLNet.Instruments;
using QLNet.Math.RandomNumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;

namespace QLNet.PricingEngines.vanilla
{
    //! Pricing engine for vanilla options using Monte Carlo simulation
    /*! \ingroup vanillaengines */
    public abstract class MCVanillaEngine<MC, RNG, S> : MCVanillaEngine<MC, RNG, S, VanillaOption>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        protected MCVanillaEngine(StochasticProcess process,
            int? timeSteps,
            int? timeStepsPerYear,
            bool brownianBridge,
            bool antitheticVariate,
            bool controlVariate,
            int? requiredSamples,
            double? requiredTolerance,
            int? maxSamples,
            ulong seed)
            : base(process, timeSteps, timeStepsPerYear, brownianBridge, antitheticVariate, controlVariate, requiredSamples,
                requiredTolerance, maxSamples, seed)
        {
        }
    }

    public abstract class MCVanillaEngine<MC, RNG, S, Inst> : McSimulation<MC, RNG, S>, IGenericEngine
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        protected bool brownianBridge_;
        protected StochasticProcess process_;
        protected int? requiredSamples_, maxSamples_;
        protected double? requiredTolerance_;
        protected ulong seed_;
        protected int? timeSteps_, timeStepsPerYear_;

        protected MCVanillaEngine(StochasticProcess process,
            int? timeSteps,
            int? timeStepsPerYear,
            bool brownianBridge,
            bool antitheticVariate,
            bool controlVariate,
            int? requiredSamples,
            double? requiredTolerance,
            int? maxSamples,
            ulong seed)
            : base(antitheticVariate, controlVariate)
        {
            process_ = process;
            timeSteps_ = timeSteps;
            timeStepsPerYear_ = timeStepsPerYear;
            requiredSamples_ = requiredSamples;
            maxSamples_ = maxSamples;
            requiredTolerance_ = requiredTolerance;
            brownianBridge_ = brownianBridge;
            seed_ = seed;

            QLNet.Utils.QL_REQUIRE(timeSteps != null || timeStepsPerYear != null, () => "no time steps provided");
            QLNet.Utils.QL_REQUIRE(timeSteps == null || timeStepsPerYear == null, () =>
                "both time steps and time steps per year were provided");
            if (timeSteps != null)
            {
                QLNet.Utils.QL_REQUIRE(timeSteps > 0, () => "timeSteps must be positive, " + timeSteps + " not allowed");
            }

            if (timeStepsPerYear != null)
            {
                QLNet.Utils.QL_REQUIRE(timeStepsPerYear > 0, () =>
                    "timeStepsPerYear must be positive, " + timeStepsPerYear + " not allowed");
            }

            process_.registerWith(update);
        }

        public virtual void calculate()
        {
            calculate(requiredTolerance_, requiredSamples_, maxSamples_);
            results_.value = mcModel_.sampleAccumulator().mean();
            if (FastActivator<RNG>.Create().allowsErrorEstimate != 0)
            {
                results_.errorEstimate = mcModel_.sampleAccumulator().errorEstimate();
            }
        }

        protected override double? controlVariateValue()
        {
            var controlPE = controlPricingEngine() as AnalyticHestonHullWhiteEngine;
            QLNet.Utils.QL_REQUIRE(controlPE != null, () => "engine does not provide control variation pricing engine");

            var controlArguments = controlPE.getArguments() as QLNet.Option.Arguments;
            QLNet.Utils.QL_REQUIRE(controlArguments != null, () => "engine is using inconsistent arguments");

            controlPE.setupArguments(arguments_);
            controlPE.calculate();

            var controlResults = controlPE.getResults() as OneAssetOption.Results;
            QLNet.Utils.QL_REQUIRE(controlResults != null, () => "engine returns an inconsistent result ExerciseType");

            return controlResults.value;
        }

        protected override IPathGenerator<IRNG> pathGenerator()
        {
            var dimensions = process_.factors();
            var grid = timeGrid();
            var generator = FastActivator<RNG>.Create().make_sequence_generator(dimensions * (grid.size() - 1), seed_);
            if (typeof(MC) == typeof(SingleVariate))
            {
                return new PathGenerator<IRNG>(process_, grid, generator, brownianBridge_);
            }

            return new MultiPathGenerator<IRNG>(process_, grid, generator, brownianBridge_);
        }

        // McSimulation implementation
        protected override TimeGrid timeGrid()
        {
            var lastExerciseDate = arguments_.exercise.lastDate();
            var t = process_.time(lastExerciseDate);
            if (timeSteps_ != null)
            {
                return new TimeGrid(t, timeSteps_.Value);
            }

            if (timeStepsPerYear_ != null)
            {
                var steps = (int)(timeStepsPerYear_ * t);
                return new TimeGrid(t, System.Math.Max(steps, 1));
            }

            QLNet.Utils.QL_FAIL("time steps not specified");
            return null;
        }

        #region PricingEngine

        protected QLNet.Option.Arguments arguments_ = new QLNet.Option.Arguments();
        protected OneAssetOption.Results results_ = new OneAssetOption.Results();

        public IPricingEngineArguments getArguments() => arguments_;

        public IPricingEngineResults getResults() => results_;

        public void reset()
        {
            results_.reset();
        }

        #region Observer & Observable

        // observable interface
        private readonly WeakEventSource eventSource = new WeakEventSource();

        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }

        public void registerWith(Callback handler)
        {
            notifyObserversEvent += handler;
        }

        public void unregisterWith(Callback handler)
        {
            notifyObserversEvent -= handler;
        }

        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        public void update()
        {
            notifyObservers();
        }

        #endregion

        #endregion
    }
}
