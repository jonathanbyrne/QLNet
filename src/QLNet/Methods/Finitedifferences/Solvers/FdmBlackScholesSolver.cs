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

using JetBrains.Annotations;
using QLNet.Methods.Finitedifferences.Operators;
using QLNet.Methods.Finitedifferences.Utilities;
using QLNet.Patterns;
using QLNet.Processes;

namespace QLNet.Methods.Finitedifferences.Solvers
{
    [PublicAPI]
    public class FdmBlackScholesSolver : LazyObject
    {
        protected double? illegalLocalVolOverwrite_;
        protected bool localVol_;
        protected Handle<GeneralizedBlackScholesProcess> process_;
        protected Handle<FdmQuantoHelper> quantoHelper_;
        protected FdmSchemeDesc schemeDesc_;
        protected Fdm1DimSolver solver_;
        protected FdmSolverDesc solverDesc_;
        protected double strike_;

        public FdmBlackScholesSolver(
            Handle<GeneralizedBlackScholesProcess> process,
            double strike,
            FdmSolverDesc solverDesc,
            FdmSchemeDesc schemeDesc = null,
            bool localVol = false,
            double? illegalLocalVolOverwrite = null,
            Handle<FdmQuantoHelper> quantoHelper = null)
        {
            process_ = process;
            strike_ = strike;
            solverDesc_ = solverDesc;
            schemeDesc_ = schemeDesc ?? new FdmSchemeDesc().Douglas();
            localVol_ = localVol;
            illegalLocalVolOverwrite_ = illegalLocalVolOverwrite;
            quantoHelper_ = quantoHelper;
            quantoHelper_ = quantoHelper ?? new Handle<FdmQuantoHelper>();

            process_.registerWith(update);
            quantoHelper_.registerWith(update);
        }

        public double deltaAt(double s)
        {
            calculate();
            return solver_.derivativeX(System.Math.Log(s)) / s;
        }

        public double gammaAt(double s)
        {
            calculate();
            return (solver_.derivativeXX(System.Math.Log(s))
                    - solver_.derivativeX(System.Math.Log(s))) / (s * s);
        }

        public double thetaAt(double s) => solver_.thetaAt(System.Math.Log(s));

        public double valueAt(double s)
        {
            calculate();
            return solver_.interpolateAt(System.Math.Log(s));
        }

        protected override void performCalculations()
        {
            var op = new FdmBlackScholesOp(
                solverDesc_.mesher, process_.currentLink(), strike_,
                localVol_, illegalLocalVolOverwrite_, 0,
                quantoHelper_.empty()
                    ? null
                    : quantoHelper_.currentLink());

            solver_ = new Fdm1DimSolver(solverDesc_, schemeDesc_, op);
        }
    }
}
