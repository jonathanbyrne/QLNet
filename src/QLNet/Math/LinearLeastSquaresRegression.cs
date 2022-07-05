/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)

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
using QLNet.Math.MatrixUtilities;

namespace QLNet.Math
{
    //! general linear least squares regression
    /*! References:
       "Numerical Recipes in C", 2nd edition,
        Press, Teukolsky, Vetterling, Flannery,

        \test the correctness of the returned values is tested by
              checking their properties.
    */
    [PublicAPI]
    public class LinearLeastSquaresRegression : LinearLeastSquaresRegression<double>
    {
        public LinearLeastSquaresRegression(List<double> x, List<double> y, List<Func<double, double>> v)
            : base(x, y, v)
        {
        }
    }

    [PublicAPI]
    public class LinearLeastSquaresRegression<ArgumentType>
    {
        private Vector a_, err_, residuals_, standardErrors_;

        public LinearLeastSquaresRegression(List<ArgumentType> x, List<double> y, List<Func<ArgumentType, double>> v)
        {
            a_ = new Vector(v.Count, 0);
            err_ = new Vector(v.Count, 0);
            residuals_ = new Vector(x.Count, 0);
            standardErrors_ = new Vector(v.Count, 0);

            QLNet.Utils.QL_REQUIRE(x.Count == y.Count, () => "sample set need to be of the same size");
            QLNet.Utils.QL_REQUIRE(x.Count >= v.Count, () => "sample set is too small");

            int i;
            var n = x.Count;
            var m = v.Count;

            var A = new Matrix(n, m);
            for (i = 0; i < m; ++i)
            {
                x.ForEach((jj, xx) => A[jj, i] = v[i](xx));
            }

            var svd = new SVD(A);
            var V = svd.V();
            var U = svd.U();
            var w = svd.singularValues();
            var threshold = n * Const.QL_EPSILON;

            for (i = 0; i < m; ++i)
            {
                if (w[i] > threshold)
                {
                    double u = 0;
                    U.column(i).ForEach((ii, vv) => u += vv * y[ii]);
                    u /= w[i];

                    for (var j = 0; j < m; ++j)
                    {
                        a_[j] += u * V[j, i];
                        err_[j] += V[j, i] * V[j, i] / (w[i] * w[i]);
                    }
                }
            }

            err_ = Vector.Sqrt(err_);
            residuals_ = A * a_ - new Vector(y);

            var chiSq = residuals_.Sum(r => r * r);
            err_.ForEach((ii, vv) => standardErrors_[ii] = vv * System.Math.Sqrt(chiSq / (n - 2)));
        }

        public Vector coefficients() => a_;
        //! modeling uncertainty as definied in Numerical Recipes

        public Vector error() => err_;

        public Vector residuals() => residuals_;

        //! standard parameter errors as given by Excel, R etc.
        public Vector standardErrors() => standardErrors_;
    }

    //! linear regression y_i = a_0 + a_1*x_0 +..+a_n*x_{n-1} + eps
}
