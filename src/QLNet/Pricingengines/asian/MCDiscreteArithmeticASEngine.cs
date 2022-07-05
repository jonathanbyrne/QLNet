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

using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math.RandomNumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Processes;

namespace QLNet.PricingEngines.asian
{
    /// <summary>
    ///     Monte Carlo pricing engine for discrete arithmetic average-strike Asian
    /// </summary>
    /// <typeparam name="RNG"></typeparam>
    /// <typeparam name="S"></typeparam>
    [PublicAPI]
    public class MCDiscreteArithmeticASEngine<RNG, S>
        : MCDiscreteAveragingAsianEngine<RNG, S>
        where RNG : IRSG, new()
        where S : Statistics, new()
    {
        // constructor
        public MCDiscreteArithmeticASEngine(
            GeneralizedBlackScholesProcess process,
            bool brownianBridge,
            bool antitheticVariate,
            int requiredSamples,
            double requiredTolerance,
            int maxSamples,
            ulong seed)
            : base(process, 1, brownianBridge, antitheticVariate, false,
                requiredSamples, requiredTolerance, maxSamples, seed)
        {
        }

        protected override PathPricer<IPath> pathPricer()
        {
            var payoff = (PlainVanillaPayoff)arguments_.payoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var exercise = (EuropeanExercise)arguments_.exercise;
            QLNet.Utils.QL_REQUIRE(exercise != null, () => "wrong exercise given");

            return (PathPricer<IPath>)new ArithmeticASOPathPricer(
                payoff.optionType(),
                process_.riskFreeRate().link.discount(timeGrid().Last()),
                arguments_.runningAccumulator.GetValueOrDefault(),
                arguments_.pastFixings.GetValueOrDefault());
        }
    }
}
