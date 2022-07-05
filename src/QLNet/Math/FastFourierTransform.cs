/*
 Copyright (C) 2018 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available online at <http://qlnet.sourceforge.net/License.html>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;

namespace QLNet.Math
{
    // FFT implementation
    [PublicAPI]
    public class FastFourierTransform
    {
        protected Vector cs_, sn_;

        public FastFourierTransform(int order)
        {
            var m = 1 << order;
            cs_ = new Vector(order);
            sn_ = new Vector(order);
            cs_[order - 1] = System.Math.Cos(2.0 * System.Math.PI / m);
            sn_[order - 1] = System.Math.Sin(2.0 * System.Math.PI / m);

            for (var i = order - 1; i > 0; --i)
            {
                cs_[i - 1] = cs_[i] * cs_[i] - sn_[i] * sn_[i];
                sn_[i - 1] = 2.0 * sn_[i] * cs_[i];
            }
        }

        public static int bit_reverse(int x, int order)
        {
            var n = 0;
            for (var i = 0; i < order; ++i)
            {
                n <<= 1;
                n |= x & 1;
                x >>= 1;
            }

            return n;
        }

        // the minimum order required for the given input size
        public static int min_order(int inputSize) => (int)System.Math.Ceiling(System.Math.Log(Convert.ToDouble(inputSize)) / System.Math.Log(2.0));

        // Inverse FFT transform.
        /* The output sequence must be allocated by the user. */
        public void inverse_transform(List<Complex> input,
            int inputBeg,
            int inputEnd,
            List<Complex> output)
        {
            transform_impl(input, inputBeg, inputEnd, output, true);
        }

        // The required size for the output vector
        public int output_size() => 1 << cs_.size();

        // FFT transform.
        /* The output sequence must be allocated by the user */
        public void transform(List<Complex> input,
            int inputBeg,
            int inputEnd,
            List<Complex> output)
        {
            transform_impl(input, inputBeg, inputEnd, output, false);
        }

        protected void transform_impl(List<Complex> input,
            int inputBeg,
            int inputEnd,
            List<Complex> output,
            bool inverse)
        {
            var order = cs_.size();
            var N = 1 << order;

            int i;
            for (i = inputBeg; i < inputEnd; ++i)
            {
                output[bit_reverse(i, order)] = new Complex(input[i].Real, input[i].Imaginary);
            }

            QLNet.Utils.QL_REQUIRE(i <= N, () => "FFT order is too small");
            for (var s = 1; s <= order; ++s)
            {
                var m = 1 << s;
                var w = new Complex(1.0, 0.0);
                var wm = new Complex(cs_[s - 1], inverse ? sn_[s - 1] : -sn_[s - 1]);
                for (var j = 0; j < m / 2; ++j)
                {
                    for (var k = j; k < N; k += m)
                    {
                        var t = w * output[k + m / 2];
                        var u = new Complex(output[k].Real, output[k].Imaginary);
                        output[k] = u + t;
                        output[k + m / 2] = u - t;
                    }

                    w *= wm;
                }
            }
        }
    }
}
