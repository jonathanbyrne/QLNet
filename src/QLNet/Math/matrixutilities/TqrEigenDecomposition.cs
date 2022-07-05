/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Extensions;

namespace QLNet.Math.MatrixUtilities
{
    //! tridiag. QR eigen decomposition with explicite shift aka Wilkinson
    /*! References:

       Wilkinson, J.H. and Reinsch, C. 1971, Linear Algebra, vol. II of
       Handbook for Automatic Computation (New York: Springer-Verlag)

       "Numerical Recipes in C", 2nd edition,
       Press, Teukolsky, Vetterling, Flannery,

       \test the correctness of the result is tested by checking it
             against known good values.
    */
    [PublicAPI]
    public class TqrEigenDecomposition
    {
        public enum EigenVectorCalculation
        {
            WithEigenVector,
            WithoutEigenVector,
            OnlyFirstRowEigenVector
        }

        public enum ShiftStrategy
        {
            NoShift,
            Overrelaxation,
            CloseEigenValue
        }

        private Vector d_;
        private Matrix ev_;
        private int iter_;

        public TqrEigenDecomposition(Vector diag, Vector sub, EigenVectorCalculation calc = EigenVectorCalculation.WithEigenVector,
            ShiftStrategy strategy = ShiftStrategy.CloseEigenValue)
        {
            iter_ = 0;
            d_ = new Vector(diag);

            var row = calc == EigenVectorCalculation.WithEigenVector ? d_.size() :
                calc == EigenVectorCalculation.WithoutEigenVector ? 0 : 1;

            ev_ = new Matrix(row, d_.size(), 0.0);

            var n = diag.size();

            QLNet.Utils.QL_REQUIRE(n == sub.size() + 1, () => "Wrong dimensions");

            var e = new Vector(n, 0.0);
            int i;
            for (i = 1; i < e.Count; ++i)
            {
                e[i] = sub[i - 1];
            }

            for (i = 0; i < ev_.rows(); ++i)
            {
                ev_[i, i] = 1.0;
            }

            for (var k = n - 1; k >= 1; --k)
            {
                while (!offDiagIsZero(k, e))
                {
                    var l = k;
                    while (--l > 0 && !offDiagIsZero(l, e))
                    {
                        ;
                    }

                    iter_++;

                    var q = d_[l];
                    if (strategy != ShiftStrategy.NoShift)
                    {
                        // calculated eigenvalue of 2x2 sub matrix of
                        // [ d_[k-1] e_[k] ]
                        // [  e_[k]  d_[k] ]
                        // which is closer to d_[k+1].
                        // FLOATING_POINT_EXCEPTION
                        var t1 = System.Math.Sqrt(
                            0.25 * (d_[k] * d_[k] + d_[k - 1] * d_[k - 1])
                            - 0.5 * d_[k - 1] * d_[k] + e[k] * e[k]);
                        var t2 = 0.5 * (d_[k] + d_[k - 1]);

                        var lambda = System.Math.Abs(t2 + t1 - d_[k]) < System.Math.Abs(t2 - t1 - d_[k]) ? t2 + t1 : t2 - t1;

                        if (strategy == ShiftStrategy.CloseEigenValue)
                        {
                            q -= lambda;
                        }
                        else
                        {
                            q -= (k == n - 1 ? 1.25 : 1.0) * lambda;
                        }
                    }

                    // the QR transformation
                    var sine = 1.0;
                    var cosine = 1.0;
                    var u = 0.0;

                    var recoverUnderflow = false;
                    for (i = l + 1; i <= k && !recoverUnderflow; ++i)
                    {
                        var h = cosine * e[i];
                        var p = sine * e[i];

                        e[i - 1] = System.Math.Sqrt(p * p + q * q);
                        if (e[i - 1].IsNotEqual(0.0))
                        {
                            sine = p / e[i - 1];
                            cosine = q / e[i - 1];

                            var g = d_[i - 1] - u;
                            var t = (d_[i] - g) * sine + 2 * cosine * h;

                            u = sine * t;
                            d_[i - 1] = g + u;
                            q = cosine * t - h;

                            for (var j = 0; j < ev_.rows(); ++j)
                            {
                                var tmp = ev_[j, i - 1];
                                ev_[j, i - 1] = sine * ev_[j, i] + cosine * tmp;
                                ev_[j, i] = cosine * ev_[j, i] - sine * tmp;
                            }
                        }
                        else
                        {
                            // recover from underflow
                            d_[i - 1] -= u;
                            e[l] = 0.0;
                            recoverUnderflow = true;
                        }
                    }

                    if (!recoverUnderflow)
                    {
                        d_[k] -= u;
                        e[k] = q;
                        e[l] = 0.0;
                    }
                }
            }

            // sort (eigenvalues, eigenvectors),
            // code taken from symmetricSchureDecomposition.cpp
            List<KeyValuePair<double, List<double>>> temp = new InitializedList<KeyValuePair<double, List<double>>>(n);
            List<double> eigenVector = new InitializedList<double>(ev_.rows());
            for (i = 0; i < n; i++)
            {
                if (ev_.rows() > 0)
                {
                    eigenVector = ev_.column(i);
                }

                temp[i] = new KeyValuePair<double, List<double>>(d_[i], eigenVector);
            }

            temp.Sort(KeyValuePairCompare);

            // first element is positive
            for (i = 0; i < n; i++)
            {
                d_[i] = temp[i].Key;
                var sign = 1.0;
                if (ev_.rows() > 0 && temp[i].Value[0] < 0.0)
                {
                    sign = -1.0;
                }

                for (var j = 0; j < ev_.rows(); ++j)
                {
                    ev_[j, i] = sign * temp[i].Value[j];
                }
            }
        }

        private static int KeyValuePairCompare(KeyValuePair<double, List<double>> a, KeyValuePair<double, List<double>> b) => a.Key.CompareTo(b.Key);

        public Vector eigenvalues() => d_;

        public Matrix eigenvectors() => ev_;

        public int iterations() => iter_;

        private bool offDiagIsZero(int k, Vector e) => (System.Math.Abs(d_[k - 1]) + System.Math.Abs(d_[k])).IsEqual(System.Math.Abs(d_[k - 1]) + System.Math.Abs(d_[k]) + System.Math.Abs(e[k]));
    }
}
