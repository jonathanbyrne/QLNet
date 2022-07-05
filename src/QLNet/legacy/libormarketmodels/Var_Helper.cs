/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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

namespace QLNet.legacy.libormarketmodels
{
    internal class Var_Helper
    {
        public int i_;
        public int j_;
        public LfmCovarianceParameterization param_;

        public Var_Helper(LfmCovarianceParameterization param, int i, int j)
        {
            param_ = param;
            i_ = i;
            j_ = j;
        }

        public virtual double value(double t)
        {
            var m = param_.diffusion(t, new Vector());
            double u = 0;
            m.row(i_).ForEach((ii, vv) => u += vv * m.row(j_)[ii]);
            return u;
        }
    }
}
