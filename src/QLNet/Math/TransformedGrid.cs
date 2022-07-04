/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using System;

namespace QLNet.Math
{
    //! transformed grid
    /*! This package encapuslates an array of grid points.  It is used primarily in PDE calculations.
    */
    [JetBrains.Annotations.PublicAPI] public class TransformedGrid
    {
        protected Vector grid_;
        protected Vector transformedGrid_;
        protected Vector dxm_;
        protected Vector dxp_;
        protected Vector dx_;

        public Vector gridArray() => grid_;

        public Vector transformedGridArray() => transformedGrid_;

        public Vector dxmArray() => dxm_;

        public Vector dxpArray() => dxp_;

        public Vector dxArray() => dx_;

        public TransformedGrid(Vector grid)
        {
            grid_ = grid.Clone();
            transformedGrid_ = grid.Clone();
            dxm_ = new Vector(grid.size());
            dxp_ = new Vector(grid.size());
            dx_ = new Vector(grid.size());

            for (var i = 1; i < transformedGrid_.size() - 1; i++)
            {
                dxm_[i] = transformedGrid_[i] - transformedGrid_[i - 1];
                dxp_[i] = transformedGrid_[i + 1] - transformedGrid_[i];
                dx_[i] = dxm_[i] + dxp_[i];
            }
        }

        public TransformedGrid(Vector grid, Func<double, double> func)
        {
            grid_ = grid.Clone();
            transformedGrid_ = new Vector(grid.size());
            dxm_ = new Vector(grid.size());
            dxp_ = new Vector(grid.size());
            dx_ = new Vector(grid.size());

            for (var i = 0; i < grid.size(); i++)
                transformedGrid_[i] = func(grid_[i]);

            for (var i = 1; i < transformedGrid_.size() - 1; i++)
            {
                dxm_[i] = transformedGrid_[i] - transformedGrid_[i - 1];
                dxp_[i] = transformedGrid_[i + 1] - transformedGrid_[i];
                dx_[i] = dxm_[i] + dxp_[i];
            }
        }

        public double grid(int i) => grid_[i];

        public double transformedGrid(int i) => transformedGrid_[i];

        public double dxm(int i) => dxm_[i];

        public double dxp(int i) => dxp_[i];

        public double dx(int i) => dx_[i];

        public int size() => grid_.size();
    }
}
