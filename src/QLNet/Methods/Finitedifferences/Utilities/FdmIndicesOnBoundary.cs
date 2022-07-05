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
using QLNet.Methods.Finitedifferences.Operators;

namespace QLNet.Methods.Finitedifferences.Utilities
{
    /// <summary>
    ///     helper class to extract the indices on a boundary
    /// </summary>
    [PublicAPI]
    public class FdmIndicesOnBoundary
    {
        protected List<int> indices_;

        public FdmIndicesOnBoundary(FdmLinearOpLayout layout,
            int direction, BoundaryCondition<FdmLinearOp>.Side side)
        {
            var newDim = new List<int>(layout.dim());
            newDim[direction] = 1;
            var hyperSize
                = newDim.accumulate(0, newDim.Count, 1,
                    (a, b) => a * b);

            indices_ = new InitializedList<int>(hyperSize);

            var i = 0;
            var endIter = layout.end();
            for (var iter = layout.begin();
                 iter != endIter;
                 ++iter)
            {
                if (side == BoundaryCondition<FdmLinearOp>.Side.Lower
                    && iter.coordinates()[direction] == 0
                    || side == BoundaryCondition<FdmLinearOp>.Side.Upper
                    && iter.coordinates()[direction]
                    == layout.dim()[direction] - 1)
                {
                    Utils.QL_REQUIRE(hyperSize > i, () => "index missmatch");
                    indices_[i++] = iter.index();
                }
            }
        }

        public List<int> getIndices() => indices_;
    }
}
