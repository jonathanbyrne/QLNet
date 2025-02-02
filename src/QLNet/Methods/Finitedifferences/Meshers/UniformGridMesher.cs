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

namespace QLNet.Methods.Finitedifferences.Meshers
{
    /// <summary>
    ///     uniform grid mesher
    /// </summary>
    [PublicAPI]
    public class UniformGridMesher : FdmMesher
    {
        protected Vector dx_;
        protected List<List<double>> locations_;

        public UniformGridMesher(FdmLinearOpLayout layout, List<Pair<double?, double?>> boundaries)
            : base(layout)
        {
            dx_ = new Vector(layout.dim().Count);
            locations_ = new InitializedList<List<double>>(layout.dim().Count);

            QLNet.Utils.QL_REQUIRE(boundaries.Count == layout.dim().Count,
                () => "inconsistent boundaries given");

            for (var i = 0; i < layout.dim().Count; ++i)
            {
                dx_[i] = (boundaries[i].second.Value - boundaries[i].first.Value)
                         / (layout.dim()[i] - 1);

                locations_[i] = new InitializedList<double>(layout.dim()[i]);
                for (var j = 0; j < layout.dim()[i]; ++j)
                {
                    locations_[i][j] = boundaries[i].first.Value + j * dx_[i];
                }
            }
        }

        public override double? dminus(FdmLinearOpIterator iter, int direction) => dx_[direction];

        public override double? dplus(FdmLinearOpIterator iter, int direction) => dx_[direction];

        public override double location(FdmLinearOpIterator iter,
            int direction) =>
            locations_[direction][iter.coordinates()[direction]];

        public override Vector locations(int direction)
        {
            var retVal = new Vector(layout_.size());

            var endIter = layout_.end();
            for (var iter = layout_.begin();
                 iter != endIter;
                 ++iter)
            {
                retVal[iter.index()] = locations_[direction][iter.coordinates()[direction]];
            }

            return retVal;
        }
    }
}
