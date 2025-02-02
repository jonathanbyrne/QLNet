﻿/*
 Copyright (C) 2008 Andrea Maggiulli

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

using Xunit;
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Methods.Finitedifferences.Operators;
using QLNet.Instruments;
using QLNet.Time;
using QLNet.Methods.Finitedifferences.Solvers;
using QLNet.Methods.Finitedifferences.Utilities;
using QLNet.Math.Interpolations;
using QLNet.Termstructures;
using QLNet.Math;
using QLNet.Math.integrals;
using QLNet.Math.MatrixUtilities;
using QLNet.Math.RandomNumbers;
using QLNet.Methods.Finitedifferences.StepConditions;
using QLNet.PricingEngines.vanilla;
using QLNet.Processes;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_FdmLinearOp
    {
        [Fact]
        public void testFdmLinearOpLayout()
        {

            var dims = new int[] { 5, 7, 8 };
            var dim = new List<int>(dims);

            var layout = new FdmLinearOpLayout(dim);

            var calculatedDim = layout.dim().Count;
            var expectedDim = dim.Count;
            if (calculatedDim != expectedDim)
            {
                QAssert.Fail("index.dimensions() should be " + expectedDim
                             + ", but is " + calculatedDim);
            }

            var calculatedSize = layout.size();
            var expectedSize = dim.accumulate(0, 3, 1, (x, y) => x * y);

            if (calculatedSize != expectedSize)
            {
                QAssert.Fail("index.size() should be "
                             + expectedSize + ", but is " + calculatedSize);
            }

            for (var k = 0; k < dim[0]; ++k)
            {
                for (var l = 0; l < dim[1]; ++l)
                {
                    for (var m = 0; m < dim[2]; ++m)
                    {
                        List<int> tmp = new InitializedList<int>(3);
                        tmp[0] = k; tmp[1] = l; tmp[2] = m;

                        var calculatedIndex = layout.index(tmp);
                        var expectedIndex = k + l * dim[0] + m * dim[0] * dim[1];

                        if (expectedIndex != layout.index(tmp))
                        {
                            QAssert.Fail("index.size() should be " + expectedIndex
                                         + ", but is " + calculatedIndex);
                        }
                    }
                }
            }

            var iter = layout.begin();

            for (var m = 0; m < dim[2]; ++m)
            {
                for (var l = 0; l < dim[1]; ++l)
                {
                    for (var k = 0; k < dim[0]; ++k, ++iter)
                    {
                        for (var n = 1; n < 4; ++n)
                        {
                            var nn = layout.neighbourhood(iter, 1, n);
                            var calculatedIndex = k + m * dim[0] * dim[1]
                                                    + (l < dim[1] - n ? l + n
                                                        : dim[1] - 1 - (l + n - (dim[1] - 1))) * dim[0];

                            if (nn != calculatedIndex)
                            {
                                QAssert.Fail("next neighbourhood index is " + nn
                                             + " but should be " + calculatedIndex);
                            }
                        }

                        for (var n = 1; n < 7; ++n)
                        {
                            var nn = layout.neighbourhood(iter, 2, -n);
                            var calculatedIndex = k + l * dim[0]
                                                    + (m < n ? n - m : m - n) * dim[0] * dim[1];
                            if (nn != calculatedIndex)
                            {
                                QAssert.Fail("next neighbourhood index is " + nn
                                             + " but should be " + calculatedIndex);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void testUniformGridMesher()
        {
            var dims = new int[] { 5, 7, 8 };
            var dim = new List<int>(dims);

            var layout = new FdmLinearOpLayout(dim);
            var boundaries = new List<Pair<double?, double?>>(); ;
            boundaries.Add(new Pair<double?, double?>(-5, 10));
            boundaries.Add(new Pair<double?, double?>(5, 100));
            boundaries.Add(new Pair<double?, double?>(10, 20));

            var mesher = new UniformGridMesher(layout, boundaries);

            var dx1 = 15.0 / (dim[0] - 1);
            var dx2 = 95.0 / (dim[1] - 1);
            var dx3 = 10.0 / (dim[2] - 1);

            var tol = 100 * Const.QL_EPSILON;
            if (System.Math.Abs(dx1 - mesher.dminus(layout.begin(), 0).Value) > tol
                || System.Math.Abs(dx1 - mesher.dplus(layout.begin(), 0).Value) > tol
                || System.Math.Abs(dx2 - mesher.dminus(layout.begin(), 1).Value) > tol
                || System.Math.Abs(dx2 - mesher.dplus(layout.begin(), 1).Value) > tol
                || System.Math.Abs(dx3 - mesher.dminus(layout.begin(), 2).Value) > tol
                || System.Math.Abs(dx3 - mesher.dplus(layout.begin(), 2).Value) > tol)
            {
                QAssert.Fail("inconsistent uniform mesher object");
            }
        }

        [Fact]
        public void testFirstDerivativesMapApply()
        {
            var dims = new int[] { 400, 100, 50 };
            var dim = new List<int>(dims);

            var index = new FdmLinearOpLayout(dim);

            var boundaries = new List<Pair<double?, double?>>();
            boundaries.Add(new Pair<double?, double?>(-5, 5));
            boundaries.Add(new Pair<double?, double?>(0, 10));
            boundaries.Add(new Pair<double?, double?>(5, 15));

            FdmMesher mesher = new UniformGridMesher(index, boundaries);

            var map = new FirstDerivativeOp(2, mesher);

            var r = new Vector(mesher.layout().size());
            var endIter = index.end();

            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                r[iter.index()] = System.Math.Sin(mesher.location(iter, 0))
                                   + System.Math.Cos(mesher.location(iter, 2));
            }

            var t = map.apply(r);
            var dz = (boundaries[2].second.Value - boundaries[2].first.Value) / (dims[2] - 1);
            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                var z = iter.coordinates()[2];

                var z0 = z > 0 ? z - 1 : 1;
                var z2 = z < dims[2] - 1 ? z + 1 : dims[2] - 2;
                var lz0 = boundaries[2].first.Value + z0 * dz;
                var lz2 = boundaries[2].first.Value + z2 * dz;

                double expected;
                if (z == 0)
                {
                    expected = (System.Math.Cos(boundaries[2].first.Value + dz)
                                - System.Math.Cos(boundaries[2].first.Value)) / dz;
                }
                else if (z == dim[2] - 1)
                {
                    expected = (System.Math.Cos(boundaries[2].second.Value)
                                - System.Math.Cos(boundaries[2].second.Value - dz)) / dz;
                }
                else
                {
                    expected = (System.Math.Cos(lz2) - System.Math.Cos(lz0)) / (2 * dz);
                }

                var calculated = t[iter.index()];
                if (System.Math.Abs(calculated - expected) > 1e-10)
                {
                    QAssert.Fail("first derivative calculation failed."
                                 + "\n    calculated: " + calculated
                                 + "\n    expected:   " + expected);
                }
            }
        }

        [Fact]
        public void testSecondDerivativesMapApply()
        {
            var dims = new int[] { 50, 50, 50 };
            var dim = new List<int>(dims);

            var index = new FdmLinearOpLayout(dim);

            var boundaries = new List<Pair<double?, double?>>();
            boundaries.Add(new Pair<double?, double?>(0, 0.5));
            boundaries.Add(new Pair<double?, double?>(0, 0.5));
            boundaries.Add(new Pair<double?, double?>(0, 0.5));

            FdmMesher mesher = new UniformGridMesher(index, boundaries);

            var r = new Vector(mesher.layout().size());
            var endIter = index.end();

            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                var x = mesher.location(iter, 0);
                var y = mesher.location(iter, 1);
                var z = mesher.location(iter, 2);

                r[iter.index()] = System.Math.Sin(x) * System.Math.Cos(y) * System.Math.Exp(z);
            }

            var t = new SecondDerivativeOp(0, mesher).apply(r);

            var tol = 5e-2;
            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                var i = iter.index();
                var x = mesher.location(iter, 0);
                var y = mesher.location(iter, 1);
                var z = mesher.location(iter, 2);

                var d = -System.Math.Sin(x) * System.Math.Cos(y) * System.Math.Exp(z);
                if (iter.coordinates()[0] == 0 || iter.coordinates()[0] == dims[0] - 1)
                {
                    d = 0;
                }

                if (System.Math.Abs(d - t[i]) > tol)
                {
                    QAssert.Fail("numerical derivative in dx^2 deviation is too big"
                                 + "\n  found at " + x + " " + y + " " + z);
                }
            }

            t = new SecondDerivativeOp(1, mesher).apply(r);
            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                var i = iter.index();
                var x = mesher.location(iter, 0);
                var y = mesher.location(iter, 1);
                var z = mesher.location(iter, 2);

                var d = -System.Math.Sin(x) * System.Math.Cos(y) * System.Math.Exp(z);
                if (iter.coordinates()[1] == 0 || iter.coordinates()[1] == dims[1] - 1)
                {
                    d = 0;
                }

                if (System.Math.Abs(d - t[i]) > tol)
                {
                    QAssert.Fail("numerical derivative in dy^2 deviation is too big"
                                 + "\n  found at " + x + " " + y + " " + z);
                }
            }

            t = new SecondDerivativeOp(2, mesher).apply(r);
            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                var i = iter.index();
                var x = mesher.location(iter, 0);
                var y = mesher.location(iter, 1);
                var z = mesher.location(iter, 2);

                var d = System.Math.Sin(x) * System.Math.Cos(y) * System.Math.Exp(z);
                if (iter.coordinates()[2] == 0 || iter.coordinates()[2] == dims[2] - 1)
                {
                    d = 0;
                }

                if (System.Math.Abs(d - t[i]) > tol)
                {
                    QAssert.Fail("numerical derivative in dz^2 deviation is too big"
                                 + "\n  found at " + x + " " + y + " " + z);
                }
            }
        }

        [Fact]
        public void testDerivativeWeightsOnNonUniformGrids()
        {
            Fdm1dMesher mesherX =
               new Concentrating1dMesher(-2.0, 3.0, 50, new Pair<double?, double?>(0.5, 0.01));
            Fdm1dMesher mesherY =
               new Concentrating1dMesher(0.5, 5.0, 25, new Pair<double?, double?>(0.5, 0.1));
            Fdm1dMesher mesherZ =
               new Concentrating1dMesher(-1.0, 2.0, 31, new Pair<double?, double?>(1.5, 0.01));

            FdmMesher meshers =
               new FdmMesherComposite(mesherX, mesherY, mesherZ);

            var layout = meshers.layout();
            var endIter = layout.end();

            var tol = 1e-13;
            for (var direction = 0; direction < 3; ++direction)
            {

                var dfdx
                   = new FirstDerivativeOp(direction, meshers).toMatrix();
                var d2fdx2
                   = new SecondDerivativeOp(direction, meshers).toMatrix();

                var gridPoints = meshers.locations(direction);

                for (var iter = layout.begin();
                     iter != endIter; ++iter)
                {

                    var c = iter.coordinates()[direction];
                    var index = iter.index();
                    var indexM1 = layout.neighbourhood(iter, direction, -1);
                    var indexP1 = layout.neighbourhood(iter, direction, +1);

                    // test only if not on the boundary
                    if (c == 0)
                    {
                        var twoPoints = new Vector(2);
                        twoPoints[0] = 0.0;
                        twoPoints[1] = gridPoints[indexP1] - gridPoints[index];

                        var ndWeights1st = new NumericalDifferentiation(x => x, 1, twoPoints).weights();

                        var beta1 = dfdx[index, index];
                        var gamma1 = dfdx[index, indexP1];
                        if (System.Math.Abs((beta1 - ndWeights1st[0]) / beta1) > tol
                            || System.Math.Abs((gamma1 - ndWeights1st[1]) / gamma1) > tol)
                        {
                            QAssert.Fail("can not reproduce the weights of the "
                                         + "first order derivative operator "
                                         + "on the lower boundary"
                                         + "\n expected beta:    " + ndWeights1st[0]
                                         + "\n calculated beta:  " + beta1
                                         + "\n difference beta:  "
                                         + (beta1 - ndWeights1st[0])
                                         + "\n expected gamma:   " + ndWeights1st[1]
                                         + "\n calculated gamma: " + gamma1
                                         + "\n difference gamma: "
                                         + (gamma1 - ndWeights1st[1]));
                        }

                        // free boundary condition by default
                        var beta2 = d2fdx2[index, index];
                        var gamma2 = d2fdx2[index, indexP1];

                        if (System.Math.Abs(beta2) > Const.QL_EPSILON
                            || System.Math.Abs(gamma2) > Const.QL_EPSILON)
                        {
                            QAssert.Fail("can not reproduce the weights of the "
                                         + "second order derivative operator "
                                         + "on the lower boundary"
                                         + "\n expected beta:    " + 0.0
                                         + "\n calculated beta:  " + beta2
                                         + "\n expected gamma:   " + 0.0
                                         + "\n calculated gamma: " + gamma2);
                        }
                    }
                    else if (c == layout.dim()[direction] - 1)
                    {
                        var twoPoints = new Vector(2);
                        twoPoints[0] = gridPoints[indexM1] - gridPoints[index];
                        twoPoints[1] = 0.0;

                        var ndWeights1st = new NumericalDifferentiation(x => x, 1, twoPoints).weights();

                        var alpha1 = dfdx[index, indexM1];
                        var beta1 = dfdx[index, index];
                        if (System.Math.Abs((alpha1 - ndWeights1st[0]) / alpha1) > tol
                            || System.Math.Abs((beta1 - ndWeights1st[1]) / beta1) > tol)
                        {
                            QAssert.Fail("can not reproduce the weights of the "
                                         + "first order derivative operator "
                                         + "on the upper boundary"
                                         + "\n expected alpha:   " + ndWeights1st[0]
                                         + "\n calculated alpha: " + alpha1
                                         + "\n difference alpha: "
                                         + (alpha1 - ndWeights1st[0])
                                         + "\n expected beta:    " + ndWeights1st[1]
                                         + "\n calculated beta:  " + beta1
                                         + "\n difference beta:  "
                                         + (beta1 - ndWeights1st[1]));
                        }

                        // free boundary condition by default
                        var alpha2 = d2fdx2[index, indexM1];
                        var beta2 = d2fdx2[index, index];

                        if (System.Math.Abs(alpha2) > Const.QL_EPSILON
                            || System.Math.Abs(beta2) > Const.QL_EPSILON)
                        {
                            QAssert.Fail("can not reproduce the weights of the "
                                         + "second order derivative operator "
                                         + "on the upper boundary"
                                         + "\n expected alpha:   " + 0.0
                                         + "\n calculated alpha: " + alpha2
                                         + "\n expected beta:    " + 0.0
                                         + "\n calculated beta:  " + beta2);
                        }
                    }
                    else
                    {
                        var threePoints = new Vector(3);
                        threePoints[0] = gridPoints[indexM1] - gridPoints[index];
                        threePoints[1] = 0.0;
                        threePoints[2] = gridPoints[indexP1] - gridPoints[index];

                        var ndWeights1st = new NumericalDifferentiation(x => x, 1, threePoints).weights();

                        var alpha1 = dfdx[index, indexM1];
                        var beta1 = dfdx[index, index];
                        var gamma1 = dfdx[index, indexP1];

                        if (System.Math.Abs((alpha1 - ndWeights1st[0]) / alpha1) > tol
                            || System.Math.Abs((beta1 - ndWeights1st[1]) / beta1) > tol
                            || System.Math.Abs((gamma1 - ndWeights1st[2]) / gamma1) > tol)
                        {
                            QAssert.Fail("can not reproduce the weights of the "
                                         + "first order derivative operator"
                                         + "\n expected alpha:   " + ndWeights1st[0]
                                         + "\n calculated alpha: " + alpha1
                                         + "\n difference alpha: "
                                         + (alpha1 - ndWeights1st[0])
                                         + "\n expected beta:    " + ndWeights1st[1]
                                         + "\n calculated beta:  " + beta1
                                         + "\n difference beta:  "
                                         + (beta1 - ndWeights1st[1])
                                         + "\n expected gamma:   " + ndWeights1st[2]
                                         + "\n calculated gamma: " + gamma1
                                         + "\n difference gamma: "
                                         + (gamma1 - ndWeights1st[2]));
                        }

                        var ndWeights2nd = new NumericalDifferentiation(x => x, 2, threePoints).weights();

                        var alpha2 = d2fdx2[index, indexM1];
                        var beta2 = d2fdx2[index, index];
                        var gamma2 = d2fdx2[index, indexP1];
                        if (System.Math.Abs((alpha2 - ndWeights2nd[0]) / alpha2) > tol
                            || System.Math.Abs((beta2 - ndWeights2nd[1]) / beta2) > tol
                            || System.Math.Abs((gamma2 - ndWeights2nd[2]) / gamma2) > tol)
                        {
                            QAssert.Fail("can not reproduce the weights of the "
                                         + "second order derivative operator"
                                         + "\n expected alpha:   " + ndWeights2nd[0]
                                         + "\n calculated alpha: " + alpha2
                                         + "\n difference alpha: "
                                         + (alpha2 - ndWeights2nd[0])
                                         + "\n expected beta:    " + ndWeights2nd[1]
                                         + "\n calculated beta:  " + beta2
                                         + "\n difference beta:  "
                                         + (beta2 - ndWeights2nd[1])
                                         + "\n expected gamma:   " + ndWeights2nd[2]
                                         + "\n calculated gamma: " + gamma2
                                         + "\n difference gamma: "
                                         + (gamma2 - ndWeights2nd[2]));
                        }
                    }
                }
            }
        }

        [Fact]
        public void testSecondOrderMixedDerivativesMapApply()
        {
            var dims = new int[] { 50, 50, 50 };
            var dim = new List<int>(dims);

            var index = new FdmLinearOpLayout(dim);

            var boundaries = new List<Pair<double?, double?>>();
            boundaries.Add(new Pair<double?, double?>(0, 0.5));
            boundaries.Add(new Pair<double?, double?>(0, 0.5));
            boundaries.Add(new Pair<double?, double?>(0, 0.5));

            FdmMesher mesher = new UniformGridMesher(index, boundaries);

            var r = new Vector(mesher.layout().size());
            var endIter = index.end();

            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                var x = mesher.location(iter, 0);
                var y = mesher.location(iter, 1);
                var z = mesher.location(iter, 2);

                r[iter.index()] = System.Math.Sin(x) * System.Math.Cos(y) * System.Math.Exp(z);
            }

            var t = new SecondOrderMixedDerivativeOp(0, 1, mesher).apply(r);
            var u = new SecondOrderMixedDerivativeOp(1, 0, mesher).apply(r);

            var tol = 5e-2;
            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                var i = iter.index();
                var x = mesher.location(iter, 0);
                var y = mesher.location(iter, 1);
                var z = mesher.location(iter, 2);

                var d = -System.Math.Cos(x) * System.Math.Sin(y) * System.Math.Exp(z);

                if (System.Math.Abs(d - t[i]) > tol)
                {
                    QAssert.Fail("numerical derivative in dxdy deviation is too big"
                                 + "\n  found at " + x + " " + y + " " + z);
                }

                if (System.Math.Abs(t[i] - u[i]) > 1e5 * Const.QL_EPSILON)
                {
                    QAssert.Fail("numerical derivative in dxdy not equal to dydx"
                                 + "\n  found at " + x + " " + y + " " + z
                                 + "\n  value    " + System.Math.Abs(t[i] - u[i]));
                }
            }

            t = new SecondOrderMixedDerivativeOp(0, 2, mesher).apply(r);
            u = new SecondOrderMixedDerivativeOp(2, 0, mesher).apply(r);
            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                var i = iter.index();
                var x = mesher.location(iter, 0);
                var y = mesher.location(iter, 1);
                var z = mesher.location(iter, 2);

                var d = System.Math.Cos(x) * System.Math.Cos(y) * System.Math.Exp(z);

                if (System.Math.Abs(d - t[i]) > tol)
                {
                    QAssert.Fail("numerical derivative in dxdy deviation is too big"
                                 + "\n  found at " + x + " " + y + " " + z);
                }

                if (System.Math.Abs(t[i] - u[i]) > 1e5 * Const.QL_EPSILON)
                {
                    QAssert.Fail("numerical derivative in dxdz not equal to dzdx"
                                 + "\n  found at " + x + " " + y + " " + z
                                 + "\n  value    " + System.Math.Abs(t[i] - u[i]));
                }
            }

            t = new SecondOrderMixedDerivativeOp(1, 2, mesher).apply(r);
            u = new SecondOrderMixedDerivativeOp(2, 1, mesher).apply(r);
            for (var iter = index.begin(); iter != endIter; ++iter)
            {
                var i = iter.index();
                var x = mesher.location(iter, 0);
                var y = mesher.location(iter, 1);
                var z = mesher.location(iter, 2);

                var d = -System.Math.Sin(x) * System.Math.Sin(y) * System.Math.Exp(z);

                if (System.Math.Abs(d - t[i]) > tol)
                {
                    QAssert.Fail("numerical derivative in dydz deviation is too big"
                                 + "\n  found at " + x + " " + y + " " + z);
                }

                if (System.Math.Abs(t[i] - u[i]) > 1e5 * Const.QL_EPSILON)
                {
                    QAssert.Fail("numerical derivative in dydz not equal to dzdy"
                                 + "\n  found at " + x + " " + y + " " + z
                                 + "\n  value    " + System.Math.Abs(t[i] - u[i]));
                }
            }


        }

        [Fact]
        public void testTripleBandMapSolve()
        {
            var dims = new int[] { 100, 400 };
            var dim = new List<int>(dims);

            var layout = new FdmLinearOpLayout(dim);

            var boundaries = new List<Pair<double?, double?>>();
            boundaries.Add(new Pair<double?, double?>(0, 1.0));
            boundaries.Add(new Pair<double?, double?>(0, 1.0));

            FdmMesher mesher = new UniformGridMesher(layout, boundaries);

            var dy = new FirstDerivativeOp(1, mesher);
            dy.axpyb(new Vector(1, 2.0), dy, dy, new Vector(1, 1.0));

            // check copy constructor
            var copyOfDy = new FirstDerivativeOp(dy);

            var u = new Vector(layout.size());
            for (var i = 0; i < layout.size(); ++i)
            {
                u[i] = System.Math.Sin(0.1 * i) + System.Math.Cos(0.35 * i);
            }

            var t = new Vector(dy.solve_splitting(copyOfDy.apply(u), 1.0, 0.0));
            for (var i = 0; i < u.size(); ++i)
            {
                if (System.Math.Abs(u[i] - t[i]) > 1e-6)
                {
                    QAssert.Fail("solve and apply are not consistent "
                                 + "\n expected      : " + u[i]
                                 + "\n calculated    : " + t[i]);
                }
            }

            var dx = new FirstDerivativeOp(0, mesher);
            dx.axpyb(new Vector(), dx, dx, new Vector(1, 1.0));

            var copyOfDx = new FirstDerivativeOp(0, mesher);
            // check assignment
            copyOfDx = dx;

            t = dx.solve_splitting(copyOfDx.apply(u), 1.0, 0.0);
            for (var i = 0; i < u.size(); ++i)
            {
                if (System.Math.Abs(u[i] - t[i]) > 1e-6)
                {
                    QAssert.Fail("solve and apply are not consistent "
                                 + "\n expected      : " + u[i]
                                 + "\n calculated    : " + t[i]);
                }
            }

            var dxx = new SecondDerivativeOp(0, mesher);
            dxx.axpyb(new Vector(1, 0.5), dxx, dx, new Vector(1, 1.0));

            // check of copy constructor
            var copyOfDxx = new SecondDerivativeOp(dxx);

            t = dxx.solve_splitting(copyOfDxx.apply(u), 1.0, 0.0);

            for (var i = 0; i < u.size(); ++i)
            {
                if (System.Math.Abs(u[i] - t[i]) > 1e-6)
                {
                    QAssert.Fail("solve and apply are not consistent "
                                 + "\n expected      : " + u[i]
                                 + "\n calculated    : " + t[i]);
                }
            }

            //check assignment operator
            copyOfDxx.add(new SecondDerivativeOp(1, mesher));
            copyOfDxx = dxx;

            t = dxx.solve_splitting(copyOfDxx.apply(u), 1.0, 0.0);

            for (var i = 0; i < u.size(); ++i)
            {
                if (System.Math.Abs(u[i] - t[i]) > 1e-6)
                {
                    QAssert.Fail("solve and apply are not consistent "
                                 + "\n expected      : " + u[i]
                                 + "\n calculated    : " + t[i]);
                }
            }
        }

        [Fact]
        public void testCrankNicolsonWithDamping()
        {
            var backup = new SavedSettings();

            DayCounter dc = new Actual360();
            var today = Date.Today;

            var spot = new SimpleQuote(100.0);
            var qTS = Utilities.flatRate(today, 0.06, dc);
            var rTS = Utilities.flatRate(today, 0.06, dc);
            var volTS = Utilities.flatVol(today, 0.35, dc);

            StrikedTypePayoff payoff =
               new CashOrNothingPayoff(QLNet.Option.Type.Put, 100, 10.0);

            var maturity = 0.75;
            var exDate = today + Convert.ToInt32(maturity * 360 + 0.5);
            Exercise exercise = new EuropeanExercise(exDate);

            var process = new
            BlackScholesMertonProcess(new Handle<Quote>(spot),
                                      new Handle<YieldTermStructure>(qTS),
                                      new Handle<YieldTermStructure>(rTS),
                                      new Handle<BlackVolTermStructure>(volTS));
            IPricingEngine engine =
               new AnalyticEuropeanEngine(process);

            var opt = new VanillaOption(payoff, exercise);
            opt.setPricingEngine(engine);
            var expectedPV = opt.NPV();
            var expectedGamma = opt.gamma();

            // fd pricing using implicit damping steps and Crank Nicolson
            int csSteps = 25, dampingSteps = 3, xGrid = 400;
            List<int> dim = new InitializedList<int>(1, xGrid);

            var layout = new FdmLinearOpLayout(dim);
            Fdm1dMesher equityMesher =
               new FdmBlackScholesMesher(
               dim[0], process, maturity, payoff.strike(),
               null, null, 0.0001, 1.5,
               new Pair<double?, double?>(payoff.strike(), 0.01));

            FdmMesher mesher =
               new FdmMesherComposite(equityMesher);

            var map =
               new FdmBlackScholesOp(mesher, process, payoff.strike());

            FdmInnerValueCalculator calculator =
               new FdmLogInnerValue(payoff, mesher, 0);

            object rhs = new Vector(layout.size());
            var x = new Vector(layout.size());
            var endIter = layout.end();

            for (var iter = layout.begin(); iter != endIter;
                 ++iter)
            {
                (rhs as Vector)[iter.index()] = calculator.avgInnerValue(iter, maturity);
                x[iter.index()] = mesher.location(iter, 0);
            }

            var solver = new FdmBackwardSolver(map, new FdmBoundaryConditionSet(),
                                                             new FdmStepConditionComposite(),
                                                             new FdmSchemeDesc().Douglas());
            solver.rollback(ref rhs, maturity, 0.0, csSteps, dampingSteps);

            var spline = new MonotonicCubicNaturalSpline(x, x.Count, rhs as Vector);

            var s = spot.value();
            var calculatedPV = spline.value(System.Math.Log(s));
            var calculatedGamma = (spline.secondDerivative(System.Math.Log(s))
                                   - spline.derivative(System.Math.Log(s))) / (s * s);

            var relTol = 2e-3;

            if (System.Math.Abs(calculatedPV - expectedPV) > relTol * expectedPV)
            {
                QAssert.Fail("Error calculating the PV of the digital option" +
                             "\n rel. tolerance:  " + relTol +
                             "\n expected:        " + expectedPV +
                             "\n calculated:      " + calculatedPV);
            }
            if (System.Math.Abs(calculatedGamma - expectedGamma) > relTol * expectedGamma)
            {
                QAssert.Fail("Error calculating the Gamma of the digital option" +
                             "\n rel. tolerance:  " + relTol +
                             "\n expected:        " + expectedGamma +
                             "\n calculated:      " + calculatedGamma);
            }
        }

        [Fact]
        public void testSpareMatrixReference()
        {
            var rows = 10;
            var columns = 10;
            var nMatrices = 5;
            var nElements = 50;

            var rng = new MersenneTwisterUniformRng(1234);

            var expected = new SparseMatrix(rows, columns);
            var refs = new List<SparseMatrix>();

            for (var i = 0; i < nMatrices; ++i)
            {
                var m = new SparseMatrix(rows, columns);
                for (var j = 0; j < nElements; ++j)
                {
                    var row = Convert.ToInt32(rng.next().value * rows);
                    var column = Convert.ToInt32(rng.next().value * columns);

                    var value = rng.next().value;
                    m[row, column] += value;
                    expected[row, column] += value;
                }

                refs.Add(m);
            }

            var calculated = refs.accumulate(1, refs.Count, refs[0], (a, b) => a + b);

            for (var i = 0; i < rows; ++i)
            {
                for (var j = 0; j < columns; ++j)
                {
                    if (System.Math.Abs(calculated[i, j] - expected[i, j]) > 100 * Const.QL_EPSILON)
                    {
                        QAssert.Fail("Error using sparse matrix references in " +
                                     "Element (" + i + ", " + j + ")" +
                                     "\n expected  : " + expected[i, j] +
                                     "\n calculated: " + calculated[i, j]);
                    }
                }
            }
        }

        [Fact]
        public void testFdmMesherIntegral()
        {
            var mesher =
               new FdmMesherComposite(
               new Concentrating1dMesher(-1, 1.6, 21, new Pair<double?, double?>(0, 0.1)),
               new Concentrating1dMesher(-3, 4, 11, new Pair<double?, double?>(1, 0.01)),
               new Concentrating1dMesher(-2, 1, 5, new Pair<double?, double?>(0.5, 0.1)));

            var layout = mesher.layout();

            var f = new Vector(mesher.layout().size());
            for (var iter = layout.begin();
                 iter != layout.end(); ++iter)
            {
                var x = mesher.location(iter, 0);
                var y = mesher.location(iter, 1);
                var z = mesher.location(iter, 2);

                f[iter.index()] = x * x + 3 * y * y - 3 * z * z
                                  + 2 * x * y - x * z - 3 * y * z
                                  + 4 * x - y - 3 * z + 2;
            }

            var tol = 1e-12;

            // Simpson's rule has to be exact here, Mathematica code gives
            // Integrate[x*x+3*y*y-3*z*z+2*x*y-x*z-3*y*z+4*x-y-3*z+2,
            //           {x, -1, 16/10}, {y, -3, 4}, {z, -2, 1}]
            var expectedSimpson = 876.512;
            var calculatedSimpson
               = new FdmMesherIntegral(mesher, new DiscreteSimpsonIntegral().value).integrate(f);

            if (System.Math.Abs(calculatedSimpson - expectedSimpson) > tol * expectedSimpson)
            {
                QAssert.Fail("discrete mesher integration using Simpson's rule failed: "
                             + "\n    calculated: " + calculatedSimpson
                             + "\n    expected:   " + expectedSimpson);
            }

            var expectedTrapezoid = 917.0148209153263;
            var calculatedTrapezoid
               = new FdmMesherIntegral(mesher, new DiscreteTrapezoidIntegral().value).integrate(f);

            if (System.Math.Abs(calculatedTrapezoid - expectedTrapezoid)
                > tol * expectedTrapezoid)
            {
                QAssert.Fail("discrete mesher integration using Trapezoid rule failed: "
                             + "\n    calculated: " + calculatedTrapezoid
                             + "\n    expected:   " + expectedTrapezoid);
            }
        }
    }
}
