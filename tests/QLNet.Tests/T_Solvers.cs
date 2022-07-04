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
using Xunit;
using QLNet.Math;
using QLNet.Math.Solvers1d;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Solvers
    {
        class Foo : ISolver1d
        {
            public override double value(double x) => x * x - 1.0;

            public override double derivative(double x) => 2.0 * x;
        }

        internal void test(Solver1D solver, string name)
        {
            var accuracy = new double[] { 1.0e-4, 1.0e-6, 1.0e-8 };
            var expected = 1.0;
            for (var i = 0; i < accuracy.Length; i++)
            {
                var root = solver.solve(new Foo(), accuracy[i], 1.5, 0.1);
                if (System.Math.Abs(root - expected) > accuracy[i])
                {
                    QAssert.Fail(name + " solver:\n"
                                 + "    expected:   " + expected + "\n"
                                 + "    calculated: " + root + "\n"
                                 + "    accuracy:   " + accuracy[i]);
                }
                root = solver.solve(new Foo(), accuracy[i], 1.5, 0.0, 1.0);
                if (System.Math.Abs(root - expected) > accuracy[i])
                {
                    QAssert.Fail(name + " solver (bracketed):\n"
                                 + "    expected:   " + expected + "\n"
                                 + "    calculated: " + root + "\n"
                                 + "    accuracy:   " + accuracy[i]);
                }
            }
        }

        [Fact]
        public void testBrent()
        {
            test(new Brent(), "Brent");
        }
        [Fact]
        public void testNewton()
        {
            test(new Newton(), "Newton");
        }
        [Fact]
        public void testFalsePosition()
        {
            test(new FalsePosition(), "FalsePosition");
        }
        [Fact]
        public void testBisection()
        {
            test(new Bisection(), "Bisection");
        }
        [Fact]
        public void testRidder()
        {
            test(new Ridder(), "Ridder");
        }
        [Fact]
        public void testSecant()
        {
            test(new Secant(), "Secant");
        }
    }
}
