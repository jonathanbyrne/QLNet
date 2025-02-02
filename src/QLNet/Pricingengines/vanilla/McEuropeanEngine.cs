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

using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math.RandomNumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    //! European option pricing engine using Monte Carlo simulation
    /*! \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              checking it against analytic results.
    */
    [PublicAPI]
    public class MCEuropeanEngine<RNG, S> : MCVanillaEngine<SingleVariate, RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        // constructor
        public MCEuropeanEngine(GeneralizedBlackScholesProcess process,
            int? timeSteps,
            int? timeStepsPerYear,
            bool brownianBridge,
            bool antitheticVariate,
            int? requiredSamples,
            double? requiredTolerance,
            int? maxSamples,
            ulong seed)
            : base(process, timeSteps, timeStepsPerYear, brownianBridge, antitheticVariate, false,
                requiredSamples, requiredTolerance, maxSamples, seed)
        {
        }

        protected override PathPricer<IPath> pathPricer()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var process = process_ as GeneralizedBlackScholesProcess;
            QLNet.Utils.QL_REQUIRE(process != null, () => "Black-Scholes process required");

            return new EuropeanPathPricer(payoff.optionType(), payoff.strike(),
                process.riskFreeRate().link.discount(timeGrid().Last()));
        }
    }

    //! Monte Carlo European engine factory
    // template <class RNG = PseudoRandom, class S = Statistics>

    [PublicAPI]
    public class MakeMCEuropeanEngine<RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        private bool antithetic_;
        private bool brownianBridge_;
        private GeneralizedBlackScholesProcess process_;
        private ulong seed_;
        private int? steps_, stepsPerYear_, samples_, maxSamples_;
        private double? tolerance_;

        public MakeMCEuropeanEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            antithetic_ = false;
            steps_ = null;
            stepsPerYear_ = null;
            samples_ = null;
            maxSamples_ = null;
            tolerance_ = null;
            brownianBridge_ = false;
            seed_ = 0;
        }

        // conversion to pricing engine
        public IPricingEngine value()
        {
            QLNet.Utils.QL_REQUIRE(steps_ != null || stepsPerYear_ != null, () => "number of steps not given");
            QLNet.Utils.QL_REQUIRE(steps_ == null || stepsPerYear_ == null, () => "number of steps overspecified");
            return new MCEuropeanEngine<RNG, S>(process_, steps_, stepsPerYear_, brownianBridge_, antithetic_,
                samples_, tolerance_, maxSamples_, seed_);
        }

        public MakeMCEuropeanEngine<RNG, S> withAbsoluteTolerance(double tolerance)
        {
            QLNet.Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            QLNet.Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () =>
                "chosen random generator policy does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }

        public MakeMCEuropeanEngine<RNG, S> withAntitheticVariate(bool b = true)
        {
            antithetic_ = b;
            return this;
        }

        public MakeMCEuropeanEngine<RNG, S> withBrownianBridge(bool brownianBridge = true)
        {
            brownianBridge_ = brownianBridge;
            return this;
        }

        public MakeMCEuropeanEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }

        public MakeMCEuropeanEngine<RNG, S> withSamples(int samples)
        {
            QLNet.Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }

        public MakeMCEuropeanEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
        }

        // named parameters
        public MakeMCEuropeanEngine<RNG, S> withSteps(int steps)
        {
            steps_ = steps;
            return this;
        }

        public MakeMCEuropeanEngine<RNG, S> withStepsPerYear(int steps)
        {
            stepsPerYear_ = steps;
            return this;
        }
    }
}
