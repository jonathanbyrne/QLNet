/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Pricingengines.asian
{
    //! Pricing engine for discrete average Asians using Monte Carlo simulation
    /*! \warning control-variate calculation is disabled under VC++6.

        \ingroup asianengines
    */

    [PublicAPI]
    public class MCDiscreteAveragingAsianEngine<RNG, S> : McSimulation<SingleVariate, RNG, S>, IGenericEngine
        //DiscreteAveragingAsianOption.Engine,
        //McSimulation<SingleVariate,RNG,S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        protected int maxTimeStepsPerYear_;
        // data members
        protected GeneralizedBlackScholesProcess process_;
        protected int requiredSamples_, maxSamples_;
        private bool brownianBridge_;
        private double requiredTolerance_;
        private ulong seed_;

        // constructor
        public MCDiscreteAveragingAsianEngine(
            GeneralizedBlackScholesProcess process,
            int maxTimeStepsPerYear,
            bool brownianBridge,
            bool antitheticVariate,
            bool controlVariate,
            int requiredSamples,
            double requiredTolerance,
            int maxSamples,
            ulong seed) : base(antitheticVariate, controlVariate)
        {
            process_ = process;
            maxTimeStepsPerYear_ = maxTimeStepsPerYear;
            requiredSamples_ = requiredSamples;
            maxSamples_ = maxSamples;
            requiredTolerance_ = requiredTolerance;
            brownianBridge_ = brownianBridge;
            seed_ = seed;
            process_.registerWith(update);
        }

        public void calculate()
        {
            calculate(requiredTolerance_, requiredSamples_, maxSamples_);
            results_.value = mcModel_.sampleAccumulator().mean();
            if (FastActivator<RNG>.Create().allowsErrorEstimate != 0)
            {
                results_.errorEstimate =
                    mcModel_.sampleAccumulator().errorEstimate();
            }
        }

        protected override double? controlVariateValue()
        {
            var controlPE = controlPricingEngine();
            Utils.QL_REQUIRE(controlPE != null, () => "engine does not provide control variation pricing engine");

            var controlArguments =
                (DiscreteAveragingAsianOption.Arguments)controlPE.getArguments();
            controlArguments = arguments_;
            controlPE.calculate();

            var controlResults =
                (OneAssetOption.Results)controlPE.getResults();

            return controlResults.value;
        }

        protected override IPathGenerator<IRNG> pathGenerator()
        {
            var grid = timeGrid();
            var gen = new RNG().make_sequence_generator(grid.size() - 1, seed_);
            return new PathGenerator<IRNG>(process_, grid,
                gen, brownianBridge_);
        }

        protected override PathPricer<IPath> pathPricer() => throw new NotImplementedException();

        // McSimulation implementation
        protected override TimeGrid timeGrid()
        {
            var referenceDate = process_.riskFreeRate().link.referenceDate();
            var voldc = process_.blackVolatility().link.dayCounter();
            List<double> fixingTimes = new InitializedList<double>(arguments_.fixingDates.Count);

            for (var i = 0; i < arguments_.fixingDates.Count; i++)
            {
                if (arguments_.fixingDates[i] >= referenceDate)
                {
                    var t = voldc.yearFraction(referenceDate,
                        arguments_.fixingDates[i]);
                    fixingTimes[i] = t;
                }
            }

            // handle here maxStepsPerYear
            return new TimeGrid(fixingTimes.Last(), fixingTimes.Count);
        }

        #region PricingEngine

        protected DiscreteAveragingAsianOption.Arguments arguments_ = new DiscreteAveragingAsianOption.Arguments();
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
