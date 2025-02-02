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
using System.Linq;
using JetBrains.Annotations;
using QLNet.Methods.Finitedifferences.Utilities;

namespace QLNet.Methods.Finitedifferences.Operators
{
    [PublicAPI]
    public class FdmLinearOpLayout
    {
        protected List<int> dim_, spacing_;
        protected int size_;

        public FdmLinearOpLayout(List<int> dim)
        {
            dim_ = dim;
            spacing_ = new InitializedList<int>(dim.Count);
            spacing_[0] = 1;

            for (var i = 0; i < dim.Count - 1; i++)
            {
                spacing_[i + 1] = dim[i] * spacing_[i];
            }

            size_ = spacing_.Last() * dim.Last();
        }

        public FdmLinearOpIterator begin() => new FdmLinearOpIterator(dim_);

        public List<int> dim() => dim_;

        public FdmLinearOpIterator end() => new FdmLinearOpIterator(size_);

        public int index(List<int> coordinates) => coordinates.inner_product(0, coordinates.Count, 0, spacing_, 0);

        public FdmLinearOpIterator iter_neighbourhood(FdmLinearOpIterator iterator, int i, int offset)
        {
            var coordinates = iterator.coordinates();

            var coorOffset = coordinates[i] + offset;
            if (coorOffset < 0)
            {
                coorOffset = -coorOffset;
            }
            else if (coorOffset >= dim_[i])
            {
                coorOffset = 2 * (dim_[i] - 1) - coorOffset;
            }

            coordinates[i] = coorOffset;

            var retVal = new FdmLinearOpIterator(dim_, coordinates,
                index(coordinates));

            return retVal;
        }

        public int neighbourhood(FdmLinearOpIterator iterator, int i, int offset)
        {
            var myIndex = iterator.index() - iterator.coordinates()[i] * spacing_[i];

            var coorOffset = iterator.coordinates()[i] + offset;

            if (coorOffset < 0)
            {
                coorOffset = -coorOffset;
            }
            else if (coorOffset >= dim_[i])
            {
                coorOffset = 2 * (dim_[i] - 1) - coorOffset;
            }

            return myIndex + coorOffset * spacing_[i];
        }

        public int neighbourhood(FdmLinearOpIterator iterator,
            int i1, int offset1,
            int i2, int offset2)
        {
            var myIndex = iterator.index()
                          - iterator.coordinates()[i1] * spacing_[i1]
                          - iterator.coordinates()[i2] * spacing_[i2];

            var coorOffset1 = iterator.coordinates()[i1] + offset1;
            if (coorOffset1 < 0)
            {
                coorOffset1 = -coorOffset1;
            }
            else if (coorOffset1 >= dim_[i1])
            {
                coorOffset1 = 2 * (dim_[i1] - 1) - coorOffset1;
            }

            var coorOffset2 = iterator.coordinates()[i2] + offset2;
            if (coorOffset2 < 0)
            {
                coorOffset2 = -coorOffset2;
            }
            else if (coorOffset2 >= dim_[i2])
            {
                coorOffset2 = 2 * (dim_[i2] - 1) - coorOffset2;
            }

            return myIndex + coorOffset1 * spacing_[i1] + coorOffset2 * spacing_[i2];
        }

        public int size() => size_;

        public List<int> spacing() => spacing_;
    }
}
