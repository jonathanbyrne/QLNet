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

namespace QLNet.Methods.Finitedifferences.Operators
{
    [PublicAPI]
    public class FdmLinearOpIterator
    {
        protected List<int> dim_, coordinates_;
        protected int index_;

        public FdmLinearOpIterator(int index = 0)
        {
            index_ = index;
            dim_ = new List<int>();
            coordinates_ = new List<int>();
        }

        public FdmLinearOpIterator(List<int> dim)
        {
            index_ = 0;
            dim_ = dim;
            coordinates_ = new InitializedList<int>(dim.Count, 0);
        }

        public FdmLinearOpIterator(List<int> dim, List<int> coordinates, int index)
        {
            index_ = index;
            dim_ = dim;
            coordinates_ = coordinates;
        }

        public FdmLinearOpIterator(FdmLinearOpIterator iter)
        {
            swap(iter);
        }

        public static bool operator ==(FdmLinearOpIterator a, FdmLinearOpIterator b) => a.index_ == b.index_;

        public static FdmLinearOpIterator operator ++(FdmLinearOpIterator a)
        {
            ++a.index_;
            for (var i = 0; i < a.dim_.Count; ++i)
            {
                if (++a.coordinates_[i] == a.dim_[i])
                {
                    a.coordinates_[i] = 0;
                }
                else
                {
                    break;
                }
            }

            return a;
        }

        public static bool operator !=(FdmLinearOpIterator a, FdmLinearOpIterator b) => a.index_ != b.index_;

        public List<int> coordinates() => coordinates_;

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var iter = obj as FdmLinearOpIterator;
            if (iter == null)
            {
                return false;
            }

            return iter.index_ == index_;
        }

        public override int GetHashCode() => 0;

        public int index() => index_;

        public void swap(FdmLinearOpIterator iter)
        {
            QLNet.Utils.swap(ref iter.index_, ref index_);
            QLNet.Utils.swap(ref iter.dim_, ref dim_);
            QLNet.Utils.swap(ref iter.coordinates_, ref coordinates_);
        }
    }
}
