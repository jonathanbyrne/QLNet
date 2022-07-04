/*
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
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;
using QLNet.Time;
using System;

namespace QLNet.Pricingengines
{
    //! Longstaff-Schwarz Monte Carlo engine for early exercise options
    /*! References:

        Francis Longstaff, Eduardo Schwartz, 2001. Valuing American Options
        by Simulation: A Simple Least-Squares Approach, The Review of
        Financial Studies, Volume 14, No. 1, 113-147

        \test the correctness of the returned value is tested by
              reproducing results available in web/literature
    */
    public abstract class MCLongstaffSchwartzEngine<GenericEngine, MC, RNG>
      : MCLongstaffSchwartzEngine<GenericEngine, MC, RNG, Statistics>
        where GenericEngine : IPricingEngine, new()
        where RNG : IRSG, new()
    {
        protected MCLongstaffSchwartzEngine(StochasticProcess process,
                                            int? timeSteps,
                                            int? timeStepsPerYear,
                                            bool brownianBridge,
                                            bool antitheticVariate,
                                            bool controlVariate,
                                            int? requiredSamples,
                                            double? requiredTolerance,
                                            int? maxSamples,
                                            ulong seed,
                                            int nCalibrationSamples) :
        base(process, timeSteps, timeStepsPerYear, brownianBridge, antitheticVariate, controlVariate,
             requiredSamples, requiredTolerance, maxSamples, seed, nCalibrationSamples)
        { }
    }

    public abstract class MCLongstaffSchwartzEngine<GenericEngine, MC, RNG, S> : McSimulation<MC, RNG, S>, IPricingEngine
       where GenericEngine : IPricingEngine, new()
       where RNG : IRSG, new()
          where S : IGeneralStatistics, new()
    {
        protected StochasticProcess process_;
        protected int? timeSteps_;
        protected int? timeStepsPerYear_;
        protected bool brownianBridge_;
        protected int? requiredSamples_;
        protected double? requiredTolerance_;
        protected int? maxSamples_;
        protected ulong seed_;
        protected int nCalibrationSamples_;
        protected bool brownianBridgeCalibration_;
        protected bool antitheticVariateCalibration_;
        protected ulong seedCalibration_;

        protected LongstaffSchwartzPathPricer<IPath> pathPricer_;


        protected MCLongstaffSchwartzEngine(StochasticProcess process,
                                            int? timeSteps,
                                            int? timeStepsPerYear,
                                            bool brownianBridge,
                                            bool antitheticVariate,
                                            bool controlVariate,
                                            int? requiredSamples,
                                            double? requiredTolerance,
                                            int? maxSamples,
                                            ulong seed,
                                            int? nCalibrationSamples)
        : base(antitheticVariate, controlVariate)
        {
            process_ = process;
            timeSteps_ = timeSteps;
            timeStepsPerYear_ = timeStepsPerYear;
            brownianBridge_ = brownianBridge;
            requiredSamples_ = requiredSamples;
            requiredTolerance_ = requiredTolerance;
            maxSamples_ = maxSamples;
            seed_ = seed;
            nCalibrationSamples_ = nCalibrationSamples ?? 2048;


            Utils.QL_REQUIRE(timeSteps != null ||
                             timeStepsPerYear != null, () => "no time steps provided");
            Utils.QL_REQUIRE(timeSteps == null ||
                             timeStepsPerYear == null, () => "both time steps and time steps per year were provided");
            Utils.QL_REQUIRE(timeSteps != 0, () =>
                             "timeSteps must be positive, " + timeSteps + " not allowed");
            Utils.QL_REQUIRE(timeStepsPerYear != 0, () =>
                             "timeStepsPerYear must be positive, " + timeStepsPerYear + " not allowed");

            process_.registerWith(update);
        }

        public virtual void calculate()
        {
            pathPricer_ = lsmPathPricer();
            mcModel_ = new MonteCarloModel<MC, RNG, S>(pathGenerator(), pathPricer_, FastActivator<S>.Create(), antitheticVariate_);

            mcModel_.addSamples(nCalibrationSamples_);
            pathPricer_.calibrate();

            calculate(requiredTolerance_, requiredSamples_, maxSamples_);
            results_.value = mcModel_.sampleAccumulator().mean();
            if (FastActivator<RNG>.Create().allowsErrorEstimate != 0)
            {
                results_.errorEstimate = mcModel_.sampleAccumulator().errorEstimate();
            }
        }

        protected override TimeGrid timeGrid()
        {
            var lastExerciseDate = arguments_.exercise.lastDate();
            var t = process_.time(lastExerciseDate);
            if (timeSteps_ != null)
            {
                return new TimeGrid(t, timeSteps_.Value);
            }
            else if (timeStepsPerYear_ != null)
            {
                var steps = (int)(timeStepsPerYear_.Value * t);
                return new TimeGrid(t, System.Math.Max(steps, 1));
            }
            else
            {
                Utils.QL_FAIL("time steps not specified");
                return null;
            }
        }

        protected override PathPricer<IPath> pathPricer()
        {
            Utils.QL_REQUIRE(pathPricer_ != null, () => "path pricer unknown");
            return pathPricer_;
        }

        protected override IPathGenerator<IRNG> pathGenerator()
        {
            var dimensions = process_.factors();
            var grid = timeGrid();
            var generator = FastActivator<RNG>.Create().make_sequence_generator(dimensions * (grid.size() - 1), seed_);
            if (typeof(MC) == typeof(SingleVariate))
                return new PathGenerator<IRNG>(process_, grid, generator, brownianBridge_);
            else
                return new MultiPathGenerator<IRNG>(process_, grid, generator, brownianBridge_);

        }

        protected abstract LongstaffSchwartzPathPricer<IPath> lsmPathPricer();

        #region PricingEngine
        protected QLNet.Option.Arguments arguments_ = new QLNet.Option.Arguments();
        protected OneAssetOption.Results results_ = new OneAssetOption.Results();

        public IPricingEngineArguments getArguments() => arguments_;

        public IPricingEngineResults getResults() => results_;

        public void reset() { results_.reset(); }

        #region Observer & Observable
        // observable interface
        private readonly WeakEventSource eventSource = new WeakEventSource();
        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }

        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        public void update() { notifyObservers(); }
        #endregion
        #endregion
    }
}
