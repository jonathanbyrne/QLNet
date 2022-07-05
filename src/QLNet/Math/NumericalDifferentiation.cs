/*
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
using JetBrains.Annotations;

namespace QLNet.Math
{
    /// <summary>
    ///     Numerical Differentiation on arbitrarily spaced grids
    ///     <remarks>
    ///         References:
    ///         B. Fornberg, 1988. Generation of Finite Difference Formulas
    ///         on Arbitrarily Spaced Grids,
    ///         http://amath.colorado.edu/faculty/fornberg/Docs/MathComp_88_FD_formulas.pdf
    ///     </remarks>
    /// </summary>
    [PublicAPI]
    public class NumericalDifferentiation
    {
        public enum Scheme
        {
            Central,
            Backward,
            Forward
        }

        protected Func<double, double> f_;
        protected Vector offsets_, w_;

        public NumericalDifferentiation(Func<double, double> f,
            int orderOfDerivative,
            Vector x_offsets)
        {
            f_ = f;
            offsets_ = x_offsets;
            w_ = calcWeights(offsets_, orderOfDerivative);
        }

        public NumericalDifferentiation(Func<double, double> f,
            int orderOfDerivative,
            double stepSize,
            int steps,
            Scheme scheme)
        {
            f_ = f;
            offsets_ = calcOffsets(stepSize, steps, scheme);
            w_ = calcWeights(offsets_, orderOfDerivative);
        }

        public Vector offsets() => offsets_;

        public double value(double x)
        {
            var s = 0.0;
            for (var i = 0; i < w_.size(); ++i)
            {
                if (System.Math.Abs(w_[i]) > Const.QL_EPSILON * Const.QL_EPSILON)
                {
                    s += w_[i] * f_(x + offsets_[i]);
                }
            }

            return s;
        }

        public Vector weights() => w_;

        protected Vector calcOffsets(double h, int n, Scheme scheme)
        {
            QLNet.Utils.QL_REQUIRE(n > 1, () => "number of steps must be greater than one");

            var retVal = new Vector(n);
            switch (scheme)
            {
                case Scheme.Central:
                    QLNet.Utils.QL_REQUIRE(n > 2 && n % 2 > 0,
                        () => "number of steps must be an odd number greater than two");
                    for (var i = 0; i < n; ++i)
                    {
                        retVal[i] = (i - n / 2) * h;
                    }

                    break;
                case Scheme.Backward:
                    for (var i = 0; i < n; ++i)
                    {
                        retVal[i] = -(i * h);
                    }

                    break;
                case Scheme.Forward:
                    for (var i = 0; i < n; ++i)
                    {
                        retVal[i] = i * h;
                    }

                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown numerical differentiation scheme");
                    break;
            }

            return retVal;
        }

        // This is a C# implementation of the algorithm/pseudo code in
        // B. Fornberg, 1998. Calculation of Weights
        //                    in Finite Difference Formulas
        // https://amath.colorado.edu/faculty/fornberg/Docs/sirev_cl.pdf
        protected Vector calcWeights(Vector x, int M)
        {
            var N = x.size();
            QLNet.Utils.QL_REQUIRE(N > M, () => "number of points must be greater "
                                                         + "than the order of the derivative");

            var d = new double[M + 1, N, N];
            d[0, 0, 0] = 1.0;
            var c1 = 1.0;

            for (var n = 1; n < N; ++n)
            {
                var c2 = 1.0;
                for (var nu = 0; nu < n; ++nu)
                {
                    var c3 = x[n] - x[nu];
                    c2 *= c3;

                    for (var m = 0; m <= System.Math.Min(n, M); ++m)
                    {
                        d[m, n, nu] = (x[n] * d[m, n - 1, nu]
                                       - (m > 0 ? m * d[m - 1, n - 1, nu] : 0.0)) / c3;
                    }
                }

                for (var m = 0; m <= M; ++m)
                {
                    d[m, n, n] = c1 / c2 * ((m > 0 ? m * d[m - 1, n - 1, n - 1] : 0.0) -
                                            x[n - 1] * d[m, n - 1, n - 1]);
                }

                c1 = c2;
            }

            var retVal = new Vector(N);
            for (var i = 0; i < N; ++i)
            {
                retVal[i] = d[M, N - 1, i];
            }

            return retVal;
        }
    }
}
