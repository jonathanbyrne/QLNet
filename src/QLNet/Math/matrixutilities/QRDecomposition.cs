﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Extensions;
using QLNet.Math.Optimization;

namespace QLNet.Math.MatrixUtilities
{
    public static partial class MatrixUtilities
    {
        //! QR decompoisition
        /*! This implementation is based on MINPACK
            (<http://www.netlib.org/minpack>,
            <http://www.netlib.org/cephes/linalg.tgz>)
  
            This subroutine uses householder transformations with column
            pivoting (optional) to compute a qr factorization of the
            m by n matrix A. That is, qrfac determines an orthogonal
            matrix q, a permutation matrix p, and an upper trapezoidal
            matrix r with diagonal elements of nonincreasing magnitude,
            such that A*p = q*r.
  
            Return value ipvt is an integer array of length n, which
            defines the permutation matrix p such that A*p = q*r.
            Column j of p is column ipvt(j) of the identity matrix.
  
            See lmdiff.cpp for further details.
        */
        public static List<int> qrDecomposition(Matrix M, ref Matrix q, ref Matrix r, bool pivot)
        {
            var mT = Matrix.transpose(M);
            var m = M.rows();
            var n = M.columns();

            List<int> lipvt = new InitializedList<int>(n);
            var rdiag = new Vector(n);
            var wa = new Vector(n);

            MINPACK.qrfac(m, n, mT, 0, (pivot) ? 1 : 0, ref lipvt, n, ref rdiag, ref rdiag, wa);

            if (r.columns() != n || r.rows() != n)
            {
                r = new Matrix(n, n);
            }

            for (var i = 0; i < n; ++i)
            {
                r[i, i] = rdiag[i];
                if (i < m)
                {
                    for (var j = i; j < mT.rows() - 1; j++)
                    {
                        r[i, j + 1] = mT[j + 1, i];
                    }
                }
            }

            if (q.rows() != m || q.columns() != n)
            {
                q = new Matrix(m, n);
            }

            var w = new Vector(m);
            for (var k = 0; k < m; ++k)
            {
                w.Erase();
                w[k] = 1.0;

                for (var j = 0; j < System.Math.Min(n, m); ++j)
                {
                    var t3 = mT[j, j];
                    if (t3.IsNotEqual(0.0))
                    {
                        double t = 0;
                        for (var kk = j; kk < mT.columns(); kk++)
                        {
                            t += (mT[j, kk] * w[kk]) / t3;
                        }

                        for (var i = j; i < m; ++i)
                        {
                            w[i] -= mT[j, i] * t;
                        }
                    }

                    q[k, j] = w[j];
                }
            }

            List<int> ipvt = new InitializedList<int>(n);
            if (pivot)
            {
                for (var i = 0; i < n; ++i)
                {
                    ipvt[i] = lipvt[i];
                }
            }
            else
            {
                for (var i = 0; i < n; ++i)
                {
                    ipvt[i] = i;
                }
            }

            return ipvt;
        }

        //! QR Solve
        /*! This implementation is based on MINPACK
            (<http://www.netlib.org/minpack>,
            <http://www.netlib.org/cephes/linalg.tgz>)
  
            Given an m by n matrix A, an n by n diagonal matrix d,
            and an m-vector b, the problem is to determine an x which
            solves the system
  
            A*x = b ,     d*x = 0 ,
  
            in the least squares sense.
  
            d is an input array of length n which must contain the
            diagonal elements of the matrix d.
  
            See lmdiff.cpp for further details.
        */
        public static Vector qrSolve(Matrix a, Vector b, bool pivot = true, Vector d = null)
        {
            var m = a.rows();
            var n = a.columns();
            if (d == null)
            {
                d = new Vector();
            }

            QLNet.Utils.QL_REQUIRE(b.Count == m, () => "dimensions of A and b don't match");
            QLNet.Utils.QL_REQUIRE(d.Count == n || d.empty(), () => "dimensions of A and d don't match");

            Matrix q = new Matrix(m, n), r = new Matrix(n, n);

            var lipvt = qrDecomposition(a, ref q, ref r, pivot);
            var ipvt = new List<int>(n);
            ipvt = lipvt;

            var aT = Matrix.transpose(a);
            var rT = Matrix.transpose(r);

            var sdiag = new Vector(n);
            var wa = new Vector(n);

            var ld = new Vector(n, 0.0);
            if (!d.empty())
            {
                ld = d;
            }

            var x = new Vector(n);
            var qtb = Matrix.transpose(q) * b;

            MINPACK.qrsolv(n, rT, n, ipvt, ld, qtb, x, sdiag, wa);

            return x;
        }
    }
}
