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

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.Interpolations;
using QLNet.Math.ODE;
using QLNet.Math.Solvers1d;
using QLNet.Methods.Finitedifferences.Utilities;

namespace QLNet.Methods.Finitedifferences.Meshers
{
    [PublicAPI]
    public class Concentrating1dMesher : Fdm1dMesher
    {
        [PublicAPI]
        public class OdeIntegrationFct
        {
            protected List<double?> points_, betas_;
            protected AdaptiveRungeKutta rk_;

            public OdeIntegrationFct(List<double?> points,
                List<double?> betas,
                double tol)
            {
                rk_ = new AdaptiveRungeKutta(tol);
                points_ = points;
                betas_ = betas;
            }

            public double solve(double a, double y0, double x0, double x1)
            {
                AdaptiveRungeKutta.OdeFct1d odeFct = (x, y) => jac(a, x, y);
                return rk_.value(odeFct, y0, x0, x1);
            }

            protected double jac(double a, double x, double y)
            {
                var s = 0.0;
                for (var i = 0; i < points_.Count; ++i)
                {
                    s += 1.0 / (betas_[i].GetValueOrDefault() + (y - points_[i].GetValueOrDefault()) *
                        (y - points_[i].GetValueOrDefault()));
                }

                return a / System.Math.Sqrt(s);
            }
        }

        [PublicAPI]
        public class OdeSolver : ISolver1d
        {
            protected OdeIntegrationFct func_;
            protected double y0_, x0_, x1_, end_;

            public OdeSolver(OdeIntegrationFct func, double y0, double x0, double x1, double end)
            {
                func_ = func;
                y0_ = y0;
                x0_ = x0;
                x1_ = x1;
                end_ = end;
            }

            public override double value(double v) => func_.solve(v, y0_, x0_, x1_) - end_;
        }

        [PublicAPI]
        public class OdeSolver2 : ISolver1d
        {
            protected Func<double, double> func_;
            protected double z_;

            public OdeSolver2(Func<double, double> func, double z)
            {
                func_ = func;
                z_ = z;
            }

            public override double value(double v) => func_(v) - z_;
        }

        public Concentrating1dMesher(double start, double end, int size,
            Pair<double?, double?> cPoints = null,
            bool requireCPoint = false)
            : base(size)
        {
            QLNet.Utils.QL_REQUIRE(end > start, () => "end must be larger than start");
            if (cPoints == null)
            {
                cPoints = new Pair<double?, double?>();
            }

            var cPoint = cPoints.first;
            var density = cPoints.second == null ? null : cPoints.second * (end - start);

            QLNet.Utils.QL_REQUIRE(cPoint == null || (cPoint >= start && cPoint <= end),
                () => "cPoint must be between start and end");
            QLNet.Utils.QL_REQUIRE(density == null || density > 0.0,
                () => "density > 0 required");
            QLNet.Utils.QL_REQUIRE(cPoint == null || density != null,
                () => "density must be given if cPoint is given");
            QLNet.Utils.QL_REQUIRE(!requireCPoint || cPoint != null,
                () => "cPoint is required in grid but not given");

            var dx = 1.0 / (size - 1);

            if (cPoint != null)
            {
                var u = new List<double>();
                var z = new List<double>();
                Interpolation transform = null;
                var c1 = Utils.Asinh((start - cPoint.Value) / density.GetValueOrDefault());
                var c2 = Utils.Asinh((end - cPoint.Value) / density.GetValueOrDefault());
                if (requireCPoint)
                {
                    u.Add(0.0);
                    z.Add(0.0);
                    if (!Math.Utils.close(cPoint.Value, start) && !Math.Utils.close(cPoint.Value, end))
                    {
                        var z0 = -c1 / (c2 - c1);
                        var u0 =
                            System.Math.Max(
                                System.Math.Min(Convert.ToInt32(z0 * (size - 1) + 0.5),
                                    Convert.ToInt32(size) - 2),
                                1) / (Convert.ToDouble(size - 1));
                        u.Add(u0);
                        z.Add(z0);
                    }

                    u.Add(1.0);
                    z.Add(1.0);
                    transform = new LinearInterpolation(u, u.Count, z);
                }

                for (var i = 1; i < size - 1; ++i)
                {
                    var li = requireCPoint ? transform.value(i * dx) : i * dx;
                    locations_[i] = cPoint.Value
                                    + density.GetValueOrDefault() * System.Math.Sinh(c1 * (1.0 - li) + c2 * li);
                }
            }
            else
            {
                for (var i = 1; i < size - 1; ++i)
                {
                    locations_[i] = start + i * dx * (end - start);
                }
            }

            locations_[0] = start;
            locations_[locations_.Count - 1] = end;

            for (var i = 0; i < size - 1; ++i)
            {
                dplus_[i] = dminus_[i + 1] = locations_[i + 1] - locations_[i];
            }

            dplus_[dplus_.Count - 1] = null;
            dminus_[0] = null;
        }

        public Concentrating1dMesher(double start, double end, int size,
            List<Tuple<double?, double?, bool>> cPoints,
            double tol = 1e-8)
            : base(size)
        {
            QLNet.Utils.QL_REQUIRE(end > start, () => "end must be larger than start");

            List<double?> points = new List<double?>(), betas = new List<double?>();
            foreach (var iter in cPoints)
            {
                points.Add(iter.Item1);
                betas.Add((iter.Item2 * (end - start)) * (iter.Item2 * (end - start)));
            }

            // get scaling factor a so that y(1) = end
            var aInit = 0.0;
            for (var i = 0; i < points.Count; ++i)
            {
                var c1 = Utils.Asinh((start - points[i].GetValueOrDefault()) / betas[i].GetValueOrDefault());
                var c2 = Utils.Asinh((end - points[i].GetValueOrDefault()) / betas[i].GetValueOrDefault());
                aInit += (c2 - c1) / points.Count;
            }

            var fct = new OdeIntegrationFct(points, betas, tol);
            var a = new Brent().solve(
                new OdeSolver(fct, start, 0.0, 1.0, end),
                tol, aInit, 0.1 * aInit);

            // solve ODE for all grid points
            Vector x = new Vector(size), y = new Vector(size);
            x[0] = 0.0;
            y[0] = start;
            var dx = 1.0 / (size - 1);
            for (var i = 1; i < size; ++i)
            {
                x[i] = i * dx;
                y[i] = fct.solve(a, y[i - 1], x[i - 1], x[i]);
            }

            // eliminate numerical noise and ensure y(1) = end
            var dy = y[y.Count - 1] - end;
            for (var i = 1; i < size; ++i)
            {
                y[i] -= i * dx * dy;
            }

            var odeSolution = new LinearInterpolation(x, x.Count, y);

            // ensure required points are part of the grid
            List<Pair<double?, double?>> w =
                new InitializedList<Pair<double?, double?>>(1, new Pair<double?, double?>(0.0, 0.0));

            for (var i = 0; i < points.Count; ++i)
            {
                if (cPoints[i].Item3 && points[i] > start && points[i] < end)
                {
                    var j = y.distance(y[0], y.BinarySearch(points[i].Value));

                    var e = new Brent().solve(
                        new OdeSolver2(odeSolution.value, points[i].Value),
                        Const.QL_EPSILON, x[j], 0.5 / size);

                    w.Add(new Pair<double?, double?>(System.Math.Min(x[size - 2], x[j]), e));
                }
            }

            w.Add(new Pair<double?, double?>(1.0, 1.0));
            w = w.OrderBy(xx => xx.first).Distinct(new equal_on_first()).ToList();

            List<double> u = new List<double>(w.Count), z = new List<double>(w.Count);
            for (var i = 0; i < w.Count; ++i)
            {
                u[i] = w[i].first.GetValueOrDefault();
                z[i] = w[i].second.GetValueOrDefault();
            }

            var transform = new LinearInterpolation(u, u.Count, z);

            for (var i = 0; i < size; ++i)
            {
                locations_[i] = odeSolution.value(transform.value(i * dx));
            }

            for (var i = 0; i < size - 1; ++i)
            {
                dplus_[i] = dminus_[i + 1] = locations_[i + 1] - locations_[i];
            }

            dplus_[dplus_.Count] = null;
            dminus_[0] = null;
        }
    }
}
