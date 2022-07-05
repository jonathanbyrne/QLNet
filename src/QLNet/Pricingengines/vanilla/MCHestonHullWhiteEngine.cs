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
using QLNet.Math.RandomNumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Models.Equity;
using QLNet.Models.Shortrate.Onefactormodels;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class MCHestonHullWhiteEngine<RNG, S> : MCVanillaEngine<MultiVariate, RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        public MCHestonHullWhiteEngine(HybridHestonHullWhiteProcess process,
            int? timeSteps,
            int? timeStepsPerYear,
            bool antitheticVariate,
            bool controlVariate,
            int? requiredSamples,
            double? requiredTolerance,
            int? maxSamples,
            ulong seed)
            : base(process, timeSteps, timeStepsPerYear, false, antitheticVariate, controlVariate, requiredSamples,
                requiredTolerance, maxSamples, seed)
        {
            process_ = process;
        }

        public override void calculate()
        {
            base.calculate();

            if (controlVariate_)
            {
                // control variate might lead to small negative
                // option values for deep OTM options
                results_.value = System.Math.Max(0.0, results_.value.GetValueOrDefault());
            }
        }

        protected override IPathGenerator<IRNG> controlPathGenerator()
        {
            var dimensions = process_.factors();
            var grid = timeGrid();
            var generator = new RNG().make_sequence_generator(dimensions * (grid.size() - 1), seed_);
            var process = process_ as HybridHestonHullWhiteProcess;
            QLNet.Utils.QL_REQUIRE(process != null, () => "invalid process");
            var cvProcess = new HybridHestonHullWhiteProcess(process.hestonProcess(),
                process.hullWhiteProcess(), 0.0, process.discretization());

            return new MultiPathGenerator<IRNG>(cvProcess, grid, generator, false);
        }

        protected override PathPricer<IPath> controlPathPricer()
        {
            var process = process_ as HybridHestonHullWhiteProcess;
            QLNet.Utils.QL_REQUIRE(process != null, () => "invalid process");

            var hestonProcess = process.hestonProcess();

            QLNet.Utils.QL_REQUIRE(hestonProcess != null, () =>
                "first constituent of the joint stochastic process need to be of ExerciseType HestonProcess");

            var exercise = arguments_.exercise;

            QLNet.Utils.QL_REQUIRE(exercise.ExerciseType() == Exercise.Type.European, () => "only european exercise is supported");

            var exerciseTime = process.time(exercise.lastDate());

            return new HestonHullWhitePathPricer(exerciseTime, arguments_.payoff, process);
        }

        protected override IPricingEngine controlPricingEngine()
        {
            var process = process_ as HybridHestonHullWhiteProcess;
            QLNet.Utils.QL_REQUIRE(process != null, () => "invalid process");

            var hestonProcess = process.hestonProcess();

            var hullWhiteProcess = process.hullWhiteProcess();

            var hestonModel = new HestonModel(hestonProcess);

            var hwModel = new HullWhite(hestonProcess.riskFreeRate(),
                hullWhiteProcess.a(),
                hullWhiteProcess.sigma());

            return new AnalyticHestonHullWhiteEngine(hestonModel, hwModel);
        }

        protected override PathPricer<IPath> pathPricer()
        {
            var exercise = arguments_.exercise;

            QLNet.Utils.QL_REQUIRE(exercise.ExerciseType() == Exercise.Type.European, () => "only european exercise is supported");

            var exerciseTime = process_.time(exercise.lastDate());

            return new HestonHullWhitePathPricer(exerciseTime, arguments_.payoff, (HybridHestonHullWhiteProcess)process_);
        }
    }

    //! Monte Carlo Heston/Hull-White engine factory
}
