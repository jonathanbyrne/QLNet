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
using QLNet.Math;
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Methods.Finitedifferences.Operators;

namespace QLNet.Methods.Finitedifferences.Utilities
{
    /// <summary>
    ///     Dirichlet boundary conditions for differential operators
    /// </summary>
    [PublicAPI]
    public class FdmDirichletBoundary : BoundaryCondition<FdmLinearOp>
    {
        protected List<int> indices_;
        protected Side side_;
        protected double valueOnBoundary_;
        protected double xExtreme_;

        public FdmDirichletBoundary(FdmMesher mesher,
            double valueOnBoundary, int direction, Side side)
        {
            side_ = side;
            valueOnBoundary_ = valueOnBoundary;
            indices_ = new FdmIndicesOnBoundary(mesher.layout(),
                direction, side).getIndices();
            if (side_ == Side.Lower)
            {
                xExtreme_ = mesher.locations(direction)[0];
            }
            else if (side_ == Side.Upper)
            {
                xExtreme_ = mesher
                    .locations(direction)[mesher.layout().dim()[direction] - 1];
            }
            else
            {
                QLNet.Utils.QL_FAIL("internal error");
            }
        }

        public override void applyAfterApplying(Vector v)
        {
            foreach (var iter in indices_)
            {
                v[iter] = valueOnBoundary_;
            }
        }

        public double applyAfterApplying(double x, double value) =>
            side_ == Side.Lower && x < xExtreme_
            || side_ == Side.Upper && x > xExtreme_
                ? valueOnBoundary_
                : value;

        public override void applyAfterSolving(Vector v)
        {
            applyAfterApplying(v);
        }

        public override void applyBeforeApplying(IOperator o)
        {
        }

        public override void applyBeforeSolving(IOperator o, Vector v)
        {
        }

        public override void setTime(double t)
        {
        }
    }
}
