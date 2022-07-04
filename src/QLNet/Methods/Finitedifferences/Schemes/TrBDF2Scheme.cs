﻿/*
 Copyright (C) 2020 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using QLNet.Math;
using QLNet.Math.matrixutilities;
using QLNet.Methods.Finitedifferences.Operators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Methods.Finitedifferences.Schemes
{
    /*! In one dimension the Crank-Nicolson scheme is equivalent to the
        Douglas scheme and in higher dimensions it is usually inferior to
        operator splitting methods like Craig-Sneyd or Hundsdorfer-Verwer.
    */
    [JetBrains.Annotations.PublicAPI] public class TrBDF2Scheme<TrapezoidalScheme> : IMixedScheme, ISchemeFactory
      where TrapezoidalScheme : class, IMixedScheme
    {
        public enum SolverType { BiCGstab, GMRES }

        public TrBDF2Scheme()
        { }

        public TrBDF2Scheme(double alpha,
                            FdmLinearOpComposite map,
                            TrapezoidalScheme trapezoidalScheme,
                            List<BoundaryCondition<FdmLinearOp>> bcSet = null,
                            double relTol = 1E-8,
                            SolverType solverType = SolverType.BiCGstab)
        {
            dt_ = null;
            beta_ = null;
            iterations_ = 0;
            alpha_ = alpha;
            map_ = map;
            trapezoidalScheme_ = trapezoidalScheme;
            bcSet_ = new BoundaryConditionSchemeHelper(bcSet);
            relTol_ = relTol;
            solverType_ = solverType;
        }

        #region ISchemeFactory

        public IMixedScheme factory(object L, object bcs, object[] additionalInputs = null)
        {
            var alpha = additionalInputs[0] as double?;
            var trapezoidalScheme = additionalInputs[1] as TrapezoidalScheme;
            var relTol = additionalInputs[0] as double?;
            var solverType = additionalInputs[2] as SolverType?;
            return new TrBDF2Scheme<TrapezoidalScheme>(alpha.Value, L as FdmLinearOpComposite, trapezoidalScheme,
                                                       bcs as List<BoundaryCondition<FdmLinearOp>>, relTol.Value, solverType.Value);
        }

        #endregion

        public void step(ref object a, double t, double theta = 1.0)
        {
            Utils.QL_REQUIRE(t - dt_ > -1e-8, () => "a step towards negative time given");
            var intermediateTimeStep = dt_.Value * alpha_;

            var fStar = a;
            trapezoidalScheme_.setStep(intermediateTimeStep);
            trapezoidalScheme_.step(ref fStar, t);

            bcSet_.setTime(System.Math.Max(0.0, t - dt_.Value));
            bcSet_.applyBeforeSolving(map_, a as Vector);

            var fStarVec = fStar as Vector;
            var f = (1.0 / alpha_ * fStarVec - System.Math.Pow(1.0 - alpha_, 2.0) / alpha_ * (a as Vector)) / (2.0 - alpha_);

            if (map_.size() == 1)
            {
                a = map_.solve_splitting(0, f, -beta_.Value);
            }
            else
            {
                if (solverType_ == SolverType.BiCGstab)
                {
                    BiCGStab.MatrixMult preconditioner = x => map_.preconditioner(x, -beta_.Value);
                    BiCGStab.MatrixMult applyF = x => apply(x);

                    var result =
                       new BiCGStab(applyF, System.Math.Max(10, (a as Vector).Count), relTol_, preconditioner).solve(f, f);

                    iterations_ += result.Iterations;
                    a = result.X;
                }
                else if (solverType_ == SolverType.GMRES)
                {
                    GMRES.MatrixMult preconditioner = x => map_.preconditioner(x, -beta_.Value);
                    GMRES.MatrixMult applyF = x => apply(x);

                    var result =
                       new GMRES(applyF, System.Math.Max(10, (a as Vector).Count) / 10, relTol_, preconditioner).solve(f, f);

                    iterations_ += result.Errors.Count;
                    a = result.X;
                }
                else
                    Utils.QL_FAIL("unknown/illegal solver ExerciseType");
            }
            bcSet_.applyAfterSolving(a as Vector);
        }

        public void setStep(double dt)
        {
            dt_ = dt;
            beta_ = (1.0 - alpha_) / (2.0 - alpha_) * dt_.Value;
        }

        public int numberOfIterations() => iterations_;

        public Vector apply(Vector r) => r - beta_.Value * map_.apply(r);

        protected double? dt_, beta_;
        protected double alpha_, relTol_;
        protected int iterations_;

        protected FdmLinearOpComposite map_;
        protected TrapezoidalScheme trapezoidalScheme_;
        protected BoundaryConditionSchemeHelper bcSet_;
        protected SolverType solverType_;
    }
}
