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
using JetBrains.Annotations;
using QLNet.Math.Interpolations;

namespace QLNet.Math
{
    //! This class contains a sampled curve.
    /*! Initially the class will contain one indexed curve */

    [PublicAPI]
    public class SampledCurve : ICloneable
    {
        private Vector grid_;
        private Vector values_;

        public SampledCurve(int gridSize)
        {
            grid_ = new Vector(gridSize);
            values_ = new Vector(gridSize);
        }

        public SampledCurve(Vector grid)
        {
            grid_ = grid.Clone();
            values_ = new Vector(grid.Count);
        }

        // instead of "=" overload
        public object Clone() => MemberwiseClone();

        public bool empty() => grid_.Count == 0;

        /*! \todo replace or complement with a more general function firstDerivativeAt(spot) */

        public double firstDerivativeAtCenter()
        {
            Utils.QL_REQUIRE(size() >= 3, () => "the size of the curve must be at least 3");

            var jmid = size() / 2;
            if (size() % 2 == 1)
            {
                return (values_[jmid + 1] - values_[jmid - 1]) / (grid_[jmid + 1] - grid_[jmid - 1]);
            }

            return (values_[jmid] - values_[jmid - 1]) / (grid_[jmid] - grid_[jmid - 1]);
        }

        public Vector grid() => grid_;

        public double gridValue(int i) => grid_[i];

        public void regrid(Vector new_grid)
        {
            var priceSpline = new CubicInterpolation(grid_, grid_.Count, values_,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0);
            priceSpline.update();
            var newValues = new Vector(new_grid.Count);

            for (var i = 0; i < new_grid.Count; i++)
            {
                newValues[i] = priceSpline.value(new_grid[i], true);
            }

            values_ = newValues;
            grid_ = new_grid.Clone();
        }

        public void regrid(Vector new_grid, Func<double, double> func)
        {
            var transformed_grid = new Vector(grid_.Count);

            for (var i = 0; i < grid_.Count; i++)
            {
                transformed_grid[i] = func(grid_[i]);
            }

            var priceSpline = new CubicInterpolation(transformed_grid, transformed_grid.Count, values_,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0);
            priceSpline.update();

            var newValues = new_grid.Clone();

            for (var i = 0; i < grid_.Count; i++)
            {
                newValues[i] = func(newValues[i]);
            }

            for (var j = 0; j < grid_.Count; j++)
            {
                newValues[j] = priceSpline.value(newValues[j], true);
            }

            values_ = newValues;
            grid_ = new_grid.Clone();
        }

        public void regridLogGrid(double min, double max)
        {
            regrid(Utils.BoundedLogGrid(min, max, size() - 1), System.Math.Log);
        }

        public void sample(Func<double, double> f)
        {
            for (var i = 0; i < grid_.Count; i++)
            {
                values_[i] = f(grid_[i]);
            }
        }

        public void scaleGrid(double s)
        {
            grid_ *= s;
        }

        /*! \todo replace or complement with a more general function secondDerivativeAt(spot) */

        public double secondDerivativeAtCenter()
        {
            Utils.QL_REQUIRE(size() >= 4, () => "the size of the curve must be at least 4");
            var jmid = size() / 2;
            if (size() % 2 == 1)
            {
                var deltaPlus = (values_[jmid + 1] - values_[jmid]) / (grid_[jmid + 1] - grid_[jmid]);
                var deltaMinus = (values_[jmid] - values_[jmid - 1]) / (grid_[jmid] - grid_[jmid - 1]);
                var dS = (grid_[jmid + 1] - grid_[jmid - 1]) / 2.0;
                return (deltaPlus - deltaMinus) / dS;
            }
            else
            {
                var deltaPlus = (values_[jmid + 1] - values_[jmid - 1]) / (grid_[jmid + 1] - grid_[jmid - 1]);
                var deltaMinus = (values_[jmid] - values_[jmid - 2]) / (grid_[jmid] - grid_[jmid - 2]);
                return (deltaPlus - deltaMinus) / (grid_[jmid] - grid_[jmid - 1]);
            }
        }

        // modifiers
        public void setGrid(Vector g)
        {
            grid_ = g.Clone();
        }

        // utilities
        public void setLogGrid(double min, double max)
        {
            setGrid(Utils.BoundedLogGrid(min, max, size() - 1));
        }

        public void setValue(int i, double v)
        {
            values_[i] = v;
        }

        public void setValues(Vector g)
        {
            values_ = g.Clone();
        }

        public void shiftGrid(double s)
        {
            grid_ += s;
        }

        public int size() => grid_.Count;

        public SampledCurve transform(Func<double, double> x)
        {
            for (var i = 0; i < values_.Count; i++)
            {
                values_[i] = x(values_[i]);
            }

            return this;
        }

        public SampledCurve transformGrid(Func<double, double> x)
        {
            for (var i = 0; i < grid_.Count; i++)
            {
                grid_[i] = x(grid_[i]);
            }

            return this;
        }

        public double value(int i) => values_[i];

        // calculations
        /*! \todo replace or complement with a more general function valueAt(spot) */

        public double valueAtCenter()
        {
            Utils.QL_REQUIRE(!empty(), () => "empty sampled curve");

            var jmid = size() / 2;
            if (size() % 2 == 1)
            {
                return values_[jmid];
            }

            return (values_[jmid] + values_[jmid - 1]) / 2.0;
        }

        public Vector values() => values_;
    }
}
