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
using QLNet.Math;
using QLNet.Methods.Finitedifferences.Operators;

namespace QLNet.Methods.Finitedifferences.Schemes
{
    //! modified Craig-Sneyd scheme

    /*! References:
        K. J. in ’t Hout and S. Foulon,
        ADI finite difference schemes for option pricing in the Heston
        model with correlation, http://arxiv.org/pdf/0811.3427
    */
    [PublicAPI]
    public class ModifiedCraigSneydScheme : IMixedScheme, ISchemeFactory
    {
        protected BoundaryConditionSchemeHelper bcSet_;
        protected double? dt_;
        protected FdmLinearOpComposite map_;
        protected double theta_, mu_;

        public ModifiedCraigSneydScheme()
        {
        }

        public ModifiedCraigSneydScheme(double theta, double mu,
            FdmLinearOpComposite map,
            List<BoundaryCondition<FdmLinearOp>> bcSet = null)
        {
            dt_ = null;
            theta_ = theta;
            mu_ = mu;
            map_ = map;
            bcSet_ = new BoundaryConditionSchemeHelper(bcSet);
        }

        #region ISchemeFactory

        public IMixedScheme factory(object L, object bcs, object[] additionalInputs)
        {
            var theta = additionalInputs[0] as double?;
            var mu = additionalInputs[1] as double?;
            return new ModifiedCraigSneydScheme(theta.Value, mu.Value,
                L as FdmLinearOpComposite, bcs as List<BoundaryCondition<FdmLinearOp>>);
        }

        #endregion

        #region IMixedScheme interface

        public void step(ref object a, double t, double theta = 1.0)
        {
            Utils.QL_REQUIRE(t - dt_.Value > -1e-8, () => "a step towards negative time given");
            map_.setTime(System.Math.Max(0.0, t - dt_.Value), t);
            bcSet_.setTime(System.Math.Max(0.0, t - dt_.Value));

            bcSet_.applyBeforeApplying(map_);
            var y = (a as Vector) + dt_.Value * map_.apply(a as Vector);
            bcSet_.applyAfterApplying(y);

            var y0 = y;

            for (var i = 0; i < map_.size(); ++i)
            {
                var rhs = y - theta_ * dt_.Value * map_.apply_direction(i, a as Vector);
                y = map_.solve_splitting(i, rhs, -theta_ * dt_.Value);
            }

            bcSet_.applyBeforeApplying(map_);
            var yt = y0 + mu_ * dt_.Value * map_.apply_mixed(y - (a as Vector))
                        + (0.5 - mu_) * dt_.Value * map_.apply(y - (a as Vector));
            ;
            bcSet_.applyAfterApplying(yt);

            for (var i = 0; i < map_.size(); ++i)
            {
                var rhs = yt - theta_ * dt_.Value * map_.apply_direction(i, a as Vector);
                yt = map_.solve_splitting(i, rhs, -theta_ * dt_.Value);
            }

            bcSet_.applyAfterSolving(yt);

            a = yt;
        }

        public void setStep(double dt)
        {
            dt_ = dt;
        }

        #endregion
    }
}
