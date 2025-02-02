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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.ODE;
using QLNet.Methods.Finitedifferences.Operators;

namespace QLNet.Methods.Finitedifferences.Schemes
{
    /*! In one dimension the Crank-Nicolson scheme is equivalent to the
        Douglas scheme and in higher dimensions it is usually inferior to
        operator splitting methods like Craig-Sneyd or Hundsdorfer-Verwer.
    */
    [PublicAPI]
    public class MethodOfLinesScheme : IMixedScheme, ISchemeFactory
    {
        protected BoundaryConditionSchemeHelper bcSet_;
        protected double? dt_;
        protected double eps_, relInitStepSize_;
        protected FdmLinearOpComposite map_;

        public MethodOfLinesScheme()
        {
        }

        public MethodOfLinesScheme(double eps,
            double relInitStepSize,
            FdmLinearOpComposite map,
            List<BoundaryCondition<FdmLinearOp>> bcSet = null)
        {
            dt_ = null;
            eps_ = eps;
            relInitStepSize_ = relInitStepSize;
            map_ = map;
            bcSet_ = new BoundaryConditionSchemeHelper(bcSet);
        }

        #region ISchemeFactory

        public IMixedScheme factory(object L, object bcs, object[] additionalInputs = null)
        {
            var eps = additionalInputs[0] as double?;
            var relInitStepSize = additionalInputs[1] as double?;
            return new MethodOfLinesScheme(eps.Value, relInitStepSize.Value,
                L as FdmLinearOpComposite, bcs as List<BoundaryCondition<FdmLinearOp>>);
        }

        #endregion

        public void setStep(double dt)
        {
            dt_ = dt;
        }

        public void step(ref object a, double t, double theta = 1.0)
        {
            QLNet.Utils.QL_REQUIRE(t - dt_ > -1e-8, () => "a step towards negative time given");
            var v = new AdaptiveRungeKutta(eps_, relInitStepSize_ * dt_.Value).value(apply, a as Vector, t, System.Math.Max(0.0, t - dt_.Value));
            var y = new Vector(v);
            bcSet_.applyAfterSolving(y);
            a = y;
        }

        protected List<double> apply(double t, List<double> r)
        {
            map_.setTime(t, t + 0.0001);
            bcSet_.applyBeforeApplying(map_);

            var dxdt = -1.0 * map_.apply(new Vector(r));

            return dxdt;
        }
    }
}
