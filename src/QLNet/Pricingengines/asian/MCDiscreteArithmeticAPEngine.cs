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
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.processes;

namespace QLNet.Pricingengines.asian
{
    //!  Monte Carlo pricing engine for discrete arithmetic average price Asian
    /*!  Monte Carlo pricing engine for discrete arithmetic average price
         Asian options. It can use MCDiscreteGeometricAPEngine (Monte Carlo
         discrete arithmetic average price engine) and
         AnalyticDiscreteGeometricAveragePriceAsianEngine (analytic discrete
         arithmetic average price engine) for control variation.

         \ingroup asianengines

         \test the correctness of the returned value is tested by
               reproducing results available in literature.
    */
    //template <class RNG = PseudoRandom, class S = Statistics>
    [PublicAPI]
    public class MCDiscreteArithmeticAPEngine<RNG, S>
        : MCDiscreteAveragingAsianEngine<RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        // constructor
        public MCDiscreteArithmeticAPEngine(
            GeneralizedBlackScholesProcess process,
            int maxTimeStepPerYear,
            bool brownianBridge,
            bool antitheticVariate,
            bool controlVariate,
            int requiredSamples,
            double requiredTolerance,
            int maxSamples,
            ulong seed)
            : base(process, maxTimeStepPerYear, brownianBridge, antitheticVariate,
                controlVariate, requiredSamples, requiredTolerance, maxSamples, seed)
        {
        }

        protected override PathPricer<IPath> controlPathPricer()
        {
            var payoff = (PlainVanillaPayoff)arguments_.payoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var exercise = (EuropeanExercise)arguments_.exercise;
            Utils.QL_REQUIRE(exercise != null, () => "wrong exercise given");

            // for seasoned option the geometric strike might be rescaled
            // to obtain an equivalent arithmetic strike.
            // Any change applied here MUST be applied to the analytic engine too
            return (PathPricer<IPath>)new GeometricAPOPathPricer(
                payoff.optionType(),
                payoff.strike(),
                process_.riskFreeRate().link.discount(timeGrid().Last()));
        }

        protected override IPricingEngine controlPricingEngine() => new AnalyticDiscreteGeometricAveragePriceAsianEngine(process_);

        protected override PathPricer<IPath> pathPricer()
        {
            var payoff = (PlainVanillaPayoff)arguments_.payoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var exercise = (EuropeanExercise)arguments_.exercise;
            Utils.QL_REQUIRE(exercise != null, () => "wrong exercise given");

            return new ArithmeticAPOPathPricer(
                payoff.optionType(),
                payoff.strike(),
                process_.riskFreeRate().link.discount(timeGrid().Last()),
                arguments_.runningAccumulator.GetValueOrDefault(),
                arguments_.pastFixings.GetValueOrDefault());
        }
    }

    //<class RNG = PseudoRandom, class S = Statistics>
}
