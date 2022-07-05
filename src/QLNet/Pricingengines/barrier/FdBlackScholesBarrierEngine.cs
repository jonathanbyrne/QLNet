﻿/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Methods.Finitedifferences;
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Methods.Finitedifferences.Operators;
using QLNet.Methods.Finitedifferences.Solvers;
using QLNet.Methods.Finitedifferences.StepConditions;
using QLNet.Methods.Finitedifferences.Utilities;
using QLNet.Pricingengines.vanilla;
using QLNet.processes;

namespace QLNet.Pricingengines.barrier
{
    /// <summary>
    ///     Finite-Differences Black Scholes barrier option engine
    /// </summary>
    [PublicAPI]
    public class FdBlackScholesBarrierEngine : DividendBarrierOption.Engine
    {
        protected double? illegalLocalVolOverwrite_;
        protected bool localVol_;
        protected GeneralizedBlackScholesProcess process_;
        protected FdmSchemeDesc schemeDesc_;
        protected int tGrid_, xGrid_, dampingSteps_;

        // Constructor
        public FdBlackScholesBarrierEngine(
            GeneralizedBlackScholesProcess process,
            int tGrid = 100, int xGrid = 100, int dampingSteps = 0,
            FdmSchemeDesc schemeDesc = null,
            bool localVol = false,
            double? illegalLocalVolOverwrite = null)
        {
            process_ = process;
            tGrid_ = tGrid;
            xGrid_ = xGrid;
            dampingSteps_ = dampingSteps;
            schemeDesc_ = schemeDesc == null ? new FdmSchemeDesc().Douglas() : schemeDesc;
            localVol_ = localVol;
            illegalLocalVolOverwrite_ = illegalLocalVolOverwrite;

            process_.registerWith(update);
        }

        public override void calculate()
        {
            // 1. Mesher
            var payoff = arguments_.payoff as StrikedTypePayoff;
            var maturity = process_.time(arguments_.exercise.lastDate());

            double? xMin = null;
            double? xMax = null;
            if (arguments_.barrierType == Barrier.Type.DownIn
                || arguments_.barrierType == Barrier.Type.DownOut)
            {
                xMin = System.Math.Log(arguments_.barrier.Value);
            }

            if (arguments_.barrierType == Barrier.Type.UpIn
                || arguments_.barrierType == Barrier.Type.UpOut)
            {
                xMax = System.Math.Log(arguments_.barrier.Value);
            }

            Fdm1dMesher equityMesher =
                new FdmBlackScholesMesher(xGrid_, process_, maturity,
                    payoff.strike(), xMin, xMax, 0.0001, 1.5,
                    new Pair<double?, double?>(),
                    arguments_.cashFlow);

            FdmMesher mesher =
                new FdmMesherComposite(equityMesher);

            // 2. Calculator
            FdmInnerValueCalculator calculator =
                new FdmLogInnerValue(payoff, mesher, 0);

            // 3. Step conditions
            var stepConditions = new List<IStepCondition<Vector>>();
            var stoppingTimes = new List<List<double>>();

            // 3.1 Step condition if discrete dividends
            var dividendCondition =
                new FdmDividendHandler(arguments_.cashFlow, mesher,
                    process_.riskFreeRate().currentLink().referenceDate(),
                    process_.riskFreeRate().currentLink().dayCounter(), 0);

            if (!arguments_.cashFlow.empty())
            {
                stepConditions.Add(dividendCondition);
                stoppingTimes.Add(dividendCondition.dividendTimes());
            }

            Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European,
                () => "only european style option are supported");

            var conditions =
                new FdmStepConditionComposite(stoppingTimes, stepConditions);

            // 4. Boundary conditions
            var boundaries = new FdmBoundaryConditionSet();
            if (arguments_.barrierType == Barrier.Type.DownIn
                || arguments_.barrierType == Barrier.Type.DownOut)
            {
                boundaries.Add(
                    new FdmDirichletBoundary(mesher, arguments_.rebate.Value, 0,
                        BoundaryCondition<FdmLinearOp>.Side.Lower));
            }

            if (arguments_.barrierType == Barrier.Type.UpIn
                || arguments_.barrierType == Barrier.Type.UpOut)
            {
                boundaries.Add(
                    new FdmDirichletBoundary(mesher, arguments_.rebate.Value, 0,
                        BoundaryCondition<FdmLinearOp>.Side.Upper));
            }

            // 5. Solver
            var solverDesc = new FdmSolverDesc();
            solverDesc.mesher = mesher;
            solverDesc.bcSet = boundaries;
            solverDesc.condition = conditions;
            solverDesc.calculator = calculator;
            solverDesc.maturity = maturity;
            solverDesc.dampingSteps = dampingSteps_;
            solverDesc.timeSteps = tGrid_;

            var solver =
                new FdmBlackScholesSolver(
                    new Handle<GeneralizedBlackScholesProcess>(process_),
                    payoff.strike(), solverDesc, schemeDesc_,
                    localVol_, illegalLocalVolOverwrite_);

            var spot = process_.x0();
            results_.value = solver.valueAt(spot);
            results_.delta = solver.deltaAt(spot);
            results_.gamma = solver.gammaAt(spot);
            results_.theta = solver.thetaAt(spot);

            // 6. Calculate vanilla option and rebate for in-barriers
            if (arguments_.barrierType == Barrier.Type.DownIn
                || arguments_.barrierType == Barrier.Type.UpIn)
            {
                // Cast the payoff
                var castedPayoff = arguments_.payoff as StrikedTypePayoff;

                // Calculate the vanilla option
                var vanillaOption =
                    new DividendVanillaOption(castedPayoff, arguments_.exercise,
                        dividendCondition.dividendDates(),
                        dividendCondition.dividends());

                vanillaOption.setPricingEngine(
                    new FdBlackScholesVanillaEngine(
                        process_, tGrid_, xGrid_,
                        0, // dampingSteps
                        schemeDesc_, localVol_, illegalLocalVolOverwrite_));

                // Calculate the rebate value
                var rebateOption =
                    new DividendBarrierOption(arguments_.barrierType,
                        arguments_.barrier.Value,
                        arguments_.rebate.Value,
                        castedPayoff, arguments_.exercise,
                        dividendCondition.dividendDates(),
                        dividendCondition.dividends());

                var min_grid_size = 50;
                var rebateDampingSteps
                    = dampingSteps_ > 0 ? System.Math.Min(1, dampingSteps_ / 2) : 0;

                rebateOption.setPricingEngine(new FdBlackScholesRebateEngine(
                    process_, tGrid_, System.Math.Max(min_grid_size, xGrid_ / 5),
                    rebateDampingSteps, schemeDesc_, localVol_,
                    illegalLocalVolOverwrite_));

                results_.value = vanillaOption.NPV() + rebateOption.NPV()
                                 - results_.value;
                results_.delta = vanillaOption.delta() + rebateOption.delta()
                                 - results_.delta;
                results_.gamma = vanillaOption.gamma() + rebateOption.gamma()
                                 - results_.gamma;
                results_.theta = vanillaOption.theta() + rebateOption.theta()
                                 - results_.theta;
            }
        }
    }
}
