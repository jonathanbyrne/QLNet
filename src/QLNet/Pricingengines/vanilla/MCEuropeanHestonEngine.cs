//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using QLNet.Instruments;
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    //! Monte Carlo Heston-model engine for European options
    /*! \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in web/literature
    */
    [PublicAPI]
    public class MCEuropeanHestonEngine<RNG, S> : MCVanillaEngine<MultiVariate, RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        public MCEuropeanHestonEngine(HestonProcess process,
            int? timeSteps,
            int? timeStepsPerYear,
            bool antitheticVariate,
            int? requiredSamples,
            double? requiredTolerance,
            int? maxSamples,
            ulong seed)
            : base(process, timeSteps, timeStepsPerYear, false, antitheticVariate, false, requiredSamples, requiredTolerance,
                maxSamples, seed)
        {
        }

        protected override PathPricer<IPath> pathPricer()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var process = process_ as HestonProcess;
            Utils.QL_REQUIRE(process != null, () => "Heston process required");

            return new EuropeanHestonPathPricer(payoff.optionType(),
                payoff.strike(),
                process.riskFreeRate().link.discount(timeGrid().Last()));
        }
    }

    //! Monte Carlo Heston European engine factory
}
