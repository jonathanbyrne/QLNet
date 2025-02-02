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

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Methods.Finitedifferences.Utilities;

namespace QLNet.Math.MatrixUtilities
{
    /*! References:
        Saad, Yousef. 1996, Iterative methods for sparse linear systems,
        http://www-users.cs.umn.edu/~saad/books.html

        Dongarra et al. 1994,
        Templates for the Solution of Linear Systems: Building Blocks
        for Iterative Methods, 2nd Edition, SIAM, Philadelphia
        http://www.netlib.org/templates/templates.pdf

        Christian Kanzow
        Numerik linearer Gleichungssysteme (German)
        Chapter 6: GMRES und verwandte Verfahren
        http://bilder.buecher.de/zusatz/12/12950/12950560_lese_1.pdf
    */

    /// <summary>
    ///     Generalized minimal residual method
    /// </summary>
    [PublicAPI]
    public class GMRES
    {
        public delegate Vector MatrixMult(Vector x);

        protected MatrixMult A_, M_;
        protected int maxIter_;
        protected double relTol_;

        public GMRES(MatrixMult A, int maxIter, double relTol,
            MatrixMult preConditioner = null)
        {
            QLNet.Utils.QL_REQUIRE(maxIter_ > 0, () => "maxIter must be greater then zero");

            A_ = A;
            M_ = preConditioner;
            maxIter_ = maxIter;
            relTol_ = relTol;
        }

        public GMRESResult solve(Vector b, Vector x0 = null)
        {
            var result = solveImpl(b, x0);

            QLNet.Utils.QL_REQUIRE(result.Errors.Last() < relTol_, () => "could not converge");

            return result;
        }

        public GMRESResult solveWithRestart(int restart, Vector b, Vector x0 = null)
        {
            var result = solveImpl(b, x0);

            var errors = result.Errors;

            for (var i = 0; i < restart - 1 && result.Errors.Last() >= relTol_; ++i)
            {
                result = solveImpl(b, result.X);
                errors.AddRange(result.Errors);
            }

            QLNet.Utils.QL_REQUIRE(errors.Last() < relTol_, () => "could not converge");

            result.Errors = errors;
            return result;
        }

        protected GMRESResult solveImpl(Vector b, Vector x0)
        {
            var bn = Vector.Norm2(b);
            GMRESResult result;
            if (bn.IsEqual(0.0))
            {
                result = new GMRESResult(new InitializedList<double>(1, 0.0), b);
                return result;
            }

            var x = !x0.empty() ? x0 : new Vector(b.size(), 0.0);
            var r = b - A_(x);

            var g = Vector.Norm2(r);
            if (g / bn < relTol_)
            {
                result = new GMRESResult(new InitializedList<double>(1, g / bn), x);
                return result;
            }

            List<Vector> v = new InitializedList<Vector>(1, r / g);
            List<Vector> h = new InitializedList<Vector>(1, new Vector(maxIter_, 0.0));
            List<double> c = new List<double>(maxIter_ + 1),
                s = new List<double>(maxIter_ + 1),
                z = new List<double>(maxIter_ + 1);

            z[0] = g;

            List<double> errors = new InitializedList<double>(1, g / bn);

            for (var j = 0; j < maxIter_ && errors.Last() >= relTol_; ++j)
            {
                h.Add(new Vector(maxIter_, 0.0));
                var w = A_(M_ != null ? M_(v[j]) : v[j]);

                for (var i = 0; i <= j; ++i)
                {
                    h[i][j] = Vector.DotProduct(w, v[i]);
                    w -= h[i][j] * v[i];
                }

                h[j + 1][j] = Vector.Norm2(w);

                if (h[j + 1][j] < Const.QL_EPSILON * Const.QL_EPSILON)
                {
                    break;
                }

                v.Add(w / h[j + 1][j]);

                for (var i = 0; i < j; ++i)
                {
                    var h0 = c[i] * h[i][j] + s[i] * h[i + 1][j];
                    var h1 = -s[i] * h[i][j] + c[i] * h[i + 1][j];

                    h[i][j] = h0;
                    h[i + 1][j] = h1;
                }

                var nu = System.Math.Sqrt(h[j][j] * h[j][j]
                                          + h[j + 1][j] * h[j + 1][j]);

                c[j] = h[j][j] / nu;
                s[j] = h[j + 1][j] / nu;

                h[j][j] = nu;
                h[j + 1][j] = 0.0;

                z[j + 1] = -s[j] * z[j];
                z[j] = c[j] * z[j];

                errors.Add(System.Math.Abs(z[j + 1] / bn));
            }

            var k = v.Count - 1;

            var y = new Vector(k, 0.0);
            y[k - 1] = z[k - 1] / h[k - 1][k - 1];

            for (var i = k - 2; i >= 0; --i)
            {
                y[i] = (z[i] - h[i].inner_product(
                    i + 1, k, i + 1, y, 0.0)) / h[i][i];
            }

            var xm = new Vector(x.Count, 0.0);
            for (var i = 0; i < x.Count; i++)
            {
                xm[i] = v[i].inner_product(0, k, 0, y, 0.0);
            }

            xm = x + (M_ != null ? M_(xm) : xm);

            result = new GMRESResult(errors, xm);
            return result;
        }
    }
}
