/*
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
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Methods.Finitedifferences.Solvers;
using QLNet.Methods.Finitedifferences.StepConditions;
using QLNet.Methods.Finitedifferences.Utilities;
using QLNet.Models.Shortrate.Onefactormodels;
using QLNet.processes;
using QLNet.Time;

namespace QLNet.Pricingengines.vanilla
{
    [PublicAPI]
    public class FdHullWhiteSwaptionEngine : GenericModelEngine<HullWhite, Swaption.Arguments, Instrument.Results>
    {
        protected double invEps_;
        protected FdmSchemeDesc schemeDesc_;
        protected int tGrid_, xGrid_, dampingSteps_;

        public FdHullWhiteSwaptionEngine(
            HullWhite model,
            int tGrid = 100, int xGrid = 100,
            int dampingSteps = 0, double invEps = 1e-5,
            FdmSchemeDesc schemeDesc = null)
            : base(model)
        {
            tGrid_ = tGrid;
            xGrid_ = xGrid;
            dampingSteps_ = dampingSteps;
            schemeDesc_ = schemeDesc == null ? new FdmSchemeDesc().Douglas() : schemeDesc;
            invEps_ = invEps;
        }

        public override void calculate()
        {
            // 1. Term structure
            var ts = model_.currentLink().termStructure();

            // 2. Mesher
            var dc = ts.currentLink().dayCounter();
            var referenceDate = ts.currentLink().referenceDate();
            var maturity = dc.yearFraction(referenceDate,
                arguments_.exercise.lastDate());

            var process = new OrnsteinUhlenbeckProcess(model_.currentLink().a(), model_.currentLink().sigma());

            Fdm1dMesher shortRateMesher =
                new FdmSimpleProcess1DMesher(xGrid_, process, maturity, 1, invEps_);

            FdmMesher mesher = new FdmMesherComposite(shortRateMesher);

            // 3. Inner Value Calculator
            var exerciseDates = arguments_.exercise.dates();
            var t2d = new Dictionary<double, Date>();

            for (var i = 0; i < exerciseDates.Count; ++i)
            {
                var t = dc.yearFraction(referenceDate, exerciseDates[i]);
                Utils.QL_REQUIRE(t >= 0, () => "exercise dates must not contain past date");

                t2d.Add(t, exerciseDates[i]);
            }

            var disTs = model_.currentLink().termStructure();
            var fwdTs
                = arguments_.swap.iborIndex().forwardingTermStructure();

            Utils.QL_REQUIRE(fwdTs.currentLink().dayCounter() == disTs.currentLink().dayCounter(),
                () => "day counter of forward and discount curve must match");
            Utils.QL_REQUIRE(fwdTs.currentLink().referenceDate() == disTs.currentLink().referenceDate(),
                () => "reference date of forward and discount curve must match");

            var fwdModel =
                new HullWhite(fwdTs, model_.currentLink().a(), model_.currentLink().sigma());

            FdmInnerValueCalculator calculator =
                new FdmAffineModelSwapInnerValue<HullWhite>(
                    model_.currentLink(), fwdModel,
                    arguments_.swap, t2d, mesher, 0);

            // 4. Step conditions
            var conditions =
                FdmStepConditionComposite.vanillaComposite(
                    new DividendSchedule(), arguments_.exercise,
                    mesher, calculator, referenceDate, dc);

            // 5. Boundary conditions
            var boundaries = new FdmBoundaryConditionSet();

            // 6. Solver
            var solverDesc = new FdmSolverDesc();
            solverDesc.mesher = mesher;
            solverDesc.bcSet = boundaries;
            solverDesc.condition = conditions;
            solverDesc.calculator = calculator;
            solverDesc.maturity = maturity;
            solverDesc.timeSteps = tGrid_;
            solverDesc.dampingSteps = dampingSteps_;

            var solver =
                new FdmHullWhiteSolver(model_, solverDesc, schemeDesc_);

            results_.value = solver.valueAt(0.0);
        }
    }
}
