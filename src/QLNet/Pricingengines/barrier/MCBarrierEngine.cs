/*
 Copyright (C) 2019 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available online at <http://qlnet.sourceforge.net/License.html>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Pricingengines.barrier
{
    //! Pricing engine for barrier options using Monte Carlo simulation
    /*! Uses the Brownian-bridge correction for the barrier found in
        <i>
        Going to Extremes: Correcting Simulation Bias in Exotic
        Option Valuation - D.R. Beaglehole, P.H. Dybvig and G. Zhou
        Financial Analysts Journal; Jan/Feb 1997; 53, 1. pg. 62-68
        </i>
        and
        <i>
        Simulating path-dependent options: A new approach -
        M. El Babsiri and G. Noel
        Journal of Derivatives; Winter 1998; 6, 2; pg. 65-83
        </i>

        \ingroup barrierengines

        \test the correctness of the returned value is tested by
              reproducing results available in literature.
    */
    [PublicAPI]
    public class MCBarrierEngine<RNG, S> : McSimulation<SingleVariate, RNG, S>, IGenericEngine
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        protected bool brownianBridge_;
        protected bool isBiased_;

        // data members
        protected GeneralizedBlackScholesProcess process_;
        protected int? requiredSamples_, maxSamples_;
        protected double? requiredTolerance_;
        protected ulong seed_;
        protected int? timeSteps_, timeStepsPerYear_;

        public MCBarrierEngine(GeneralizedBlackScholesProcess process,
            int? timeSteps,
            int? timeStepsPerYear,
            bool brownianBridge,
            bool antitheticVariate,
            int? requiredSamples,
            double? requiredTolerance,
            int? maxSamples,
            bool isBiased,
            ulong seed)
            : base(antitheticVariate, false)
        {
            process_ = process;
            timeSteps_ = timeSteps;
            timeStepsPerYear_ = timeStepsPerYear;
            requiredSamples_ = requiredSamples;
            maxSamples_ = maxSamples;
            requiredTolerance_ = requiredTolerance;
            isBiased_ = isBiased;
            brownianBridge_ = brownianBridge;
            seed_ = seed;

            Utils.QL_REQUIRE(timeSteps != null || timeStepsPerYear != null, () => "no time steps provided");
            Utils.QL_REQUIRE(timeSteps == null || timeStepsPerYear == null, () => "both time steps and time steps per year were provided");

            if (timeSteps != null)
            {
                Utils.QL_REQUIRE(timeSteps > 0, () => "timeSteps must be positive, " + timeSteps + " not allowed");
            }

            if (timeStepsPerYear != null)
            {
                Utils.QL_REQUIRE(timeStepsPerYear > 0, () => "timeStepsPerYear must be positive, " + timeStepsPerYear + " not allowed");
            }

            process_.registerWith(update);
        }

        public void calculate()
        {
            var spot = process_.x0();
            Utils.QL_REQUIRE(spot >= 0.0, () => "negative or null underlying given");
            Utils.QL_REQUIRE(!triggered(spot), () => "barrier touched");
            calculate(requiredTolerance_,
                requiredSamples_,
                maxSamples_);
            results_.value = mcModel_.sampleAccumulator().mean();
            if (new RNG().allowsErrorEstimate > 0)
            {
                results_.errorEstimate =
                    mcModel_.sampleAccumulator().errorEstimate();
            }
        }

        protected override IPathGenerator<IRNG> pathGenerator()
        {
            var grid = timeGrid();
            var gen = new RNG().make_sequence_generator(grid.size() - 1, seed_);
            return new PathGenerator<IRNG>(process_, grid, gen, brownianBridge_);
        }

        protected override PathPricer<IPath> pathPricer()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var grid = timeGrid();
            List<double> discounts = new InitializedList<double>(grid.size());
            for (var i = 0; i < grid.size(); i++)
            {
                discounts[i] = process_.riskFreeRate().currentLink().discount(grid[i]);
            }

            // do this with template parameters?
            if (isBiased_)
            {
                return new BiasedBarrierPathPricer(arguments_.barrierType,
                    arguments_.barrier,
                    arguments_.rebate,
                    payoff.optionType(),
                    payoff.strike(),
                    discounts);
            }

            IRNG sequenceGen = new RandomSequenceGenerator<MersenneTwisterUniformRng>(grid.size() - 1, 5);
            return new BarrierPathPricer(arguments_.barrierType,
                arguments_.barrier,
                arguments_.rebate,
                payoff.optionType(),
                payoff.strike(),
                discounts,
                process_,
                sequenceGen);
        }

        protected override TimeGrid timeGrid()
        {
            var residualTime = process_.time(arguments_.exercise.lastDate());
            if (timeSteps_ > 0)
            {
                return new TimeGrid(residualTime, timeSteps_.Value);
            }

            if (timeStepsPerYear_ > 0)
            {
                var steps = (int)(timeStepsPerYear_.Value * residualTime);
                return new TimeGrid(residualTime, System.Math.Max(steps, 1));
            }

            Utils.QL_FAIL("time steps not specified");
            return null;
        }

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
                    Utils.QL_FAIL("unknown ExerciseType");
                    return false;
            }
        }

        #region PricingEngine

        protected BarrierOption.Arguments arguments_ = new BarrierOption.Arguments();
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

    //! Monte Carlo barrier-option engine factory
}
