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
using QLNet.Methods.Finitedifferences.Operators;

namespace QLNet.Methods.Finitedifferences.Schemes
{
    [PublicAPI]
    public class BoundaryConditionSchemeHelper
    {
        protected List<BoundaryCondition<FdmLinearOp>> bcSet_;

        public BoundaryConditionSchemeHelper(List<BoundaryCondition<FdmLinearOp>> bcSet)
        {
            bcSet_ = bcSet;
        }

        public void applyAfterApplying(Vector a)
        {
            for (var i = 0; i < bcSet_.Count; ++i)
            {
                bcSet_[i].applyAfterApplying(a);
            }
        }

        public void applyAfterSolving(Vector a)
        {
            for (var i = 0; i < bcSet_.Count; ++i)
            {
                bcSet_[i].applyAfterSolving(a);
            }
        }

        //BoundaryCondition inheritance
        public void applyBeforeApplying(IOperator op)
        {
            for (var i = 0; i < bcSet_.Count; ++i)
            {
                bcSet_[i].applyBeforeApplying(op);
            }
        }

        public void applyBeforeSolving(IOperator op, Vector a)
        {
            for (var i = 0; i < bcSet_.Count; ++i)
            {
                bcSet_[i].applyBeforeSolving(op, a);
            }
        }

        public void setTime(double t)
        {
            for (var i = 0; i < bcSet_.Count; ++i)
            {
                bcSet_[i].setTime(t);
            }
        }
    }
}
