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

namespace QLNet.Methods.Finitedifferences.Meshers
{
    /// <summary>
    ///     One-dimensional simple FDM mesher object working on an index
    /// </summary>
    [PublicAPI]
    public class Fdm1dMesher
    {
        protected List<double?> dplus_, dminus_;
        protected List<double> locations_;

        public Fdm1dMesher(int size)
        {
            locations_ = new InitializedList<double>(size);
            dplus_ = new InitializedList<double?>(size);
            dminus_ = new InitializedList<double?>(size);
        }

        public double? dminus(int index) => dminus_[index];

        public double? dplus(int index) => dplus_[index];

        public double location(int index) => locations_[index];

        public List<double> locations() => locations_;

        public int size() => locations_.Count;
    }
}
