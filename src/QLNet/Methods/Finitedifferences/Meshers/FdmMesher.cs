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

using QLNet.Math;
using QLNet.Methods.Finitedifferences.Operators;

namespace QLNet.Methods.Finitedifferences.Meshers
{
    public abstract class FdmMesher
    {
        protected FdmLinearOpLayout layout_;

        public FdmMesher(FdmLinearOpLayout layout)
        {
            layout_ = layout;
        }

        public abstract double? dminus(FdmLinearOpIterator iter,
            int direction);

        public abstract double? dplus(FdmLinearOpIterator iter,
            int direction);

        public abstract double location(FdmLinearOpIterator iter,
            int direction);

        public abstract Vector locations(int direction);

        public FdmLinearOpLayout layout() => layout_;
    }
}
