﻿/*
 Copyright (C) 2018  Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available online at <https://github.com/amaggiulli/qlnetLicense.html>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using Xunit;
using System.Numerics;
using System.Collections.Generic;
using System;
using QLNet.Math;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_FastFourierTransform
    {

        [Fact]
        public void testFFTSimple()
        {
            var a = new List<Complex>();
            a.Add(new Complex(0, 0));
            a.Add(new Complex(1, 1));
            a.Add(new Complex(3, 3));
            a.Add(new Complex(4, 4));
            a.Add(new Complex(4, 4));
            a.Add(new Complex(3, 3));
            a.Add(new Complex(1, 1));
            a.Add(new Complex(0, 0));

            List<Complex> b = new InitializedList<Complex>(8);

            var fft = new FastFourierTransform(3);
            fft.transform(a, 0, 8, b);
            var expected = new List<Complex>();
            expected.Add(new Complex(16, 16));
            expected.Add(new Complex(-4.8284, -11.6569));
            expected.Add(new Complex(0, 0));
            expected.Add(new Complex(-0.3431, 0.8284));
            expected.Add(new Complex(0, 0));
            expected.Add(new Complex(0.8284, -0.3431));
            expected.Add(new Complex(0, 0));
            expected.Add(new Complex(-11.6569, -4.8284));

            for (var i = 0; i < 8; i++)
            {
                if (System.Math.Abs(b[i].Real - expected[i].Real) > 1.0e-2 ||
                    System.Math.Abs(b[i].Imaginary - expected[i].Imaginary) > 1.0e-2)
                {
                    QAssert.Fail("Convolution(" + i + ")\n"
                                 + "    calculated: " + b[i] + "\n"
                                 + "    expected:   " + expected[i]);
                }
            }
        }

        [Fact]
        public void testFFTInverse()
        {
            List<Complex> x = new InitializedList<Complex>(3);
            x[0] = 1;
            x[1] = 2;
            x[2] = 3;

            var order = FastFourierTransform.min_order(x.Count) + 1;
            var fft = new FastFourierTransform(order);

            var nFrq = fft.output_size();
            List<Complex> ft = new InitializedList<Complex>(nFrq);
            List<Complex> tmp = new InitializedList<Complex>(nFrq);
            var z = new Complex();

            fft.inverse_transform(x, 0, 3, ft);
            for (var i = 0; i < nFrq; ++i)
            {
                tmp[i] = System.Math.Pow(ft[i].Magnitude, 2.0);
                ft[i] = z;
            }

            fft.inverse_transform(tmp, 0, tmp.Count, ft);

            // 0
            var calculated = ft[0].Real / nFrq;
            var expected = (x[0] * x[0] + x[1] * x[1] + x[2] * x[2]).Real;
            if (System.Math.Abs(calculated - expected) > 1.0e-10)
            {
                QAssert.Fail("Convolution(0)\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected);
            }

            // 1
            calculated = ft[1].Real / nFrq;
            expected = (x[0] * x[1] + x[1] * x[2]).Real;
            if (System.Math.Abs(calculated - expected) > 1.0e-10)
            {
                QAssert.Fail("Convolution(1)\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected);
            }

            // 2
            calculated = ft[2].Real / nFrq;
            expected = (x[0] * x[2]).Real;
            if (System.Math.Abs(calculated - expected) > 1.0e-10)
            {
                QAssert.Fail("Convolution(1)\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected);
            }
        }
    }
}
