/*
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
using System;
using System.Collections.Generic;
using Xunit;
using QLNet;
using QLNet.Math;
using QLNet.Math.MatrixUtilities;
using QLNet.Math.RandomNumbers;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Matrices
    {

        int N;
        Matrix M1, M2, M3, M4, M5, M6, M7, I;

        double norm(Vector v) => System.Math.Sqrt(Vector.DotProduct(v, v));

        double norm(Matrix m)
        {
            var sum = 0.0;
            for (var i = 0; i < m.rows(); i++)
                for (var j = 0; j < m.columns(); j++)
                {
                    sum += m[i, j] * m[i, j];
                }

            return System.Math.Sqrt(sum);
        }

        void setup()
        {

            N = 3;
            M1 = new Matrix(N, N); M2 = new Matrix(N, N); I = new Matrix(N, N);
            M3 = new Matrix(3, 4);
            M4 = new Matrix(4, 3);
            M5 = new Matrix(4, 4);
            M6 = new Matrix(4, 4);

            M1[0, 0] = 1.0; M1[0, 1] = 0.9; M1[0, 2] = 0.7;
            M1[1, 0] = 0.9; M1[1, 1] = 1.0; M1[1, 2] = 0.4;
            M1[2, 0] = 0.7; M1[2, 1] = 0.4; M1[2, 2] = 1.0;

            M2[0, 0] = 1.0; M2[0, 1] = 0.9; M2[0, 2] = 0.7;
            M2[1, 0] = 0.9; M2[1, 1] = 1.0; M2[1, 2] = 0.3;
            M2[2, 0] = 0.7; M2[2, 1] = 0.3; M2[2, 2] = 1.0;

            I[0, 0] = 1.0; I[0, 1] = 0.0; I[0, 2] = 0.0;
            I[1, 0] = 0.0; I[1, 1] = 1.0; I[1, 2] = 0.0;
            I[2, 0] = 0.0; I[2, 1] = 0.0; I[2, 2] = 1.0;

            M3[0, 0] = 1; M3[0, 1] = 2; M3[0, 2] = 3; M3[0, 3] = 4;
            M3[1, 0] = 2; M3[1, 1] = 0; M3[1, 2] = 2; M3[1, 3] = 1;
            M3[2, 0] = 0; M3[2, 1] = 1; M3[2, 2] = 0; M3[2, 3] = 0;

            M4[0, 0] = 1; M4[0, 1] = 2; M4[0, 2] = 400;
            M4[1, 0] = 2; M4[1, 1] = 0; M4[1, 2] = 1;
            M4[2, 0] = 30; M4[2, 1] = 2; M4[2, 2] = 0;
            M4[3, 0] = 2; M4[3, 1] = 0; M4[3, 2] = 1.05;

            // from Higham - nearest correlation matrix
            M5[0, 0] = 2; M5[0, 1] = -1; M5[0, 2] = 0.0; M5[0, 3] = 0.0;
            M5[1, 0] = M5[0, 1]; M5[1, 1] = 2; M5[1, 2] = -1; M5[1, 3] = 0.0;
            M5[2, 0] = M5[0, 2]; M5[2, 1] = M5[1, 2]; M5[2, 2] = 2; M5[2, 3] = -1;
            M5[3, 0] = M5[0, 3]; M5[3, 1] = M5[1, 3]; M5[3, 2] = M5[2, 3]; M5[3, 3] = 2;

            // from Higham - nearest correlation matrix to M5
            M6[0, 0] = 1; M6[0, 1] = -0.8084124981; M6[0, 2] = 0.1915875019; M6[0, 3] = 0.106775049;
            M6[1, 0] = M6[0, 1]; M6[1, 1] = 1; M6[1, 2] = -0.6562326948; M6[1, 3] = M6[0, 2];
            M6[2, 0] = M6[0, 2]; M6[2, 1] = M6[1, 2]; M6[2, 2] = 1; M6[2, 3] = M6[0, 1];
            M6[3, 0] = M6[0, 3]; M6[3, 1] = M6[1, 3]; M6[3, 2] = M6[2, 3]; M6[3, 3] = 1;

            M7 = new Matrix(M1);
            M7[0, 1] = 0.3; M7[0, 2] = 0.2; M7[2, 1] = 1.2;
        }

        [Fact]
        public void testEigenvectors()
        {
            //("Testing eigenvalues and eigenvectors calculation...");

            setup();

            Matrix[] testMatrices = { M1, M2 };

            for (var k = 0; k < testMatrices.Length; k++)
            {

                var M = testMatrices[k];
                var dec = new SymmetricSchurDecomposition(M);
                var eigenValues = dec.eigenvalues();
                var eigenVectors = dec.eigenvectors();
                var minHolder = double.MaxValue;

                for (var i = 0; i < N; i++)
                {
                    var v = new Vector(N);
                    for (var j = 0; j < N; j++)
                    {
                        v[j] = eigenVectors[j, i];
                    }

                    // check definition
                    var a = M * v;
                    var b = eigenValues[i] * v;
                    if (norm(a - b) > 1.0e-15)
                    {
                        QAssert.Fail("Eigenvector definition not satisfied");
                    }

                    // check decreasing ordering
                    if (eigenValues[i] >= minHolder)
                    {
                        QAssert.Fail("Eigenvalues not ordered: " + eigenValues);
                    }
                    else
                    {
                        minHolder = eigenValues[i];
                    }
                }

                // check normalization
                var m = eigenVectors * Matrix.transpose(eigenVectors);
                if (norm(m - I) > 1.0e-15)
                {
                    QAssert.Fail("Eigenvector not normalized");
                }
            }
        }

        [Fact]
        public void testSqrt()
        {

            //BOOST_MESSAGE("Testing matricial square root...");

            setup();

            var m = MatrixUtilitites.pseudoSqrt(M1, MatrixUtilitites.SalvagingAlgorithm.None);
            var temp = m * Matrix.transpose(m);
            var error = norm(temp - M1);
            var tolerance = 1.0e-12;
            if (error > tolerance)
            {
                QAssert.Fail("Matrix square root calculation failed\n"
                             + "original matrix:\n" + M1
                             + "pseudoSqrt:\n" + m
                             + "pseudoSqrt*pseudoSqrt:\n" + temp
                             + "\nerror:     " + error
                             + "\ntolerance: " + tolerance);
            }
        }

        [Fact]
        public void testHighamSqrt()
        {
            //BOOST_MESSAGE("Testing Higham matricial square root...");

            setup();

            var tempSqrt = MatrixUtilitites.pseudoSqrt(M5, MatrixUtilitites.SalvagingAlgorithm.Higham);
            var ansSqrt = MatrixUtilitites.pseudoSqrt(M6, MatrixUtilitites.SalvagingAlgorithm.None);
            var error = norm(ansSqrt - tempSqrt);
            var tolerance = 1.0e-4;
            if (error > tolerance)
            {
                QAssert.Fail("Higham matrix correction failed\n"
                             + "original matrix:\n" + M5
                             + "pseudoSqrt:\n" + tempSqrt
                             + "should be:\n" + ansSqrt
                             + "\nerror:     " + error
                             + "\ntolerance: " + tolerance);
            }
        }

        [Fact]
        public void testSVD()
        {

            //BOOST_MESSAGE("Testing singular value decomposition...");

            setup();

            var tol = 1.0e-12;
            Matrix[] testMatrices = { M1, M2, M3, M4 };

            for (var j = 0; j < testMatrices.Length; j++)
            {
                // m >= n required (rows >= columns)
                var A = testMatrices[j];
                var svd = new SVD(A);
                // U is m x n
                var U = svd.U();
                // s is n long
                var s = svd.singularValues();
                // S is n x n
                var S = svd.S();
                // V is n x n
                var V = svd.V();

                for (var i = 0; i < S.rows(); i++)
                {
                    if (S[i, i] != s[i])
                    {
                        QAssert.Fail("S not consistent with s");
                    }
                }

                // tests
                var U_Utranspose = Matrix.transpose(U) * U;
                if (norm(U_Utranspose - I) > tol)
                {
                    QAssert.Fail("U not orthogonal (norm of U^T*U-I = " + norm(U_Utranspose - I) + ")");
                }

                var V_Vtranspose = Matrix.transpose(V) * V;
                if (norm(V_Vtranspose - I) > tol)
                {
                    QAssert.Fail("V not orthogonal (norm of V^T*V-I = " + norm(V_Vtranspose - I) + ")");
                }

                var A_reconstructed = U * S * Matrix.transpose(V);
                if (norm(A_reconstructed - A) > tol)
                {
                    QAssert.Fail("Product does not recover A: (norm of U*S*V^T-A = " + norm(A_reconstructed - A) + ")");
                }
            }
        }

        [Fact]
        public void testQRDecomposition()
        {

            // Testing QR decomposition...

            setup();

            var tol = 1.0e-12;
            Matrix[] testMatrices = { M1, M2, I,
                                   M3, Matrix.transpose(M3), M4, Matrix.transpose(M4), M5
                                 };

            for (var j = 0; j < testMatrices.Length; j++)
            {
                Matrix Q = new Matrix(), R = new Matrix();
                var pivot = true;
                var A = testMatrices[j];
                var ipvt = MatrixUtilities.qrDecomposition(A, ref Q, ref R, pivot);

                var P = new Matrix(A.columns(), A.columns(), 0.0);

                // reverse column pivoting
                for (var i = 0; i < P.columns(); ++i)
                {
                    P[ipvt[i], i] = 1.0;
                }

                if (norm(Q * R - A * P) > tol)
                {
                    QAssert.Fail("Q*R does not match matrix A*P (norm = "
                                 + norm(Q * R - A * P) + ")");
                }

                pivot = false;
                MatrixUtilities.qrDecomposition(A, ref Q, ref R, pivot);

                if (norm(Q * R - A) > tol)
                {
                    QAssert.Fail("Q*R does not match matrix A (norm = "
                                 + norm(Q * R - A) + ")");
                }
            }
        }

        [Fact]
        public void testQRSolve()
        {
            // Testing QR solve...
            setup();

            var tol = 1.0e-12;
            var rng = new MersenneTwisterUniformRng(1234);
            var bigM = new Matrix(50, 100, 0.0);
            for (var i = 0; i < System.Math.Min(bigM.rows(), bigM.columns()); ++i)
            {
                bigM[i, i] = i + 1.0;
            }
            Matrix[] testMatrices = { M1, M2, M3, Matrix.transpose(M3),
                                   M4, Matrix.transpose(M4), M5, I, M7, bigM, Matrix.transpose(bigM)
                                 };

            for (var j = 0; j < testMatrices.Length; j++)
            {
                var A = testMatrices[j];
                var b = new Vector(A.rows());

                for (var k = 0; k < 10; ++k)
                {
                    for (var i = 0; i < b.Count; ++i)
                    {
                        b[i] = rng.next().value;
                    }
                    var x = MatrixUtilities.qrSolve(A, b, true);

                    if (A.columns() >= A.rows())
                    {
                        if (norm(A * x - b) > tol)
                        {
                            QAssert.Fail("A*x does not match vector b (norm = "
                                         + norm(A * x - b) + ")");
                        }
                    }
                    else
                    {
                        // use the SVD to calculate the reference values
                        var n = A.columns();
                        var xr = new Vector(n, 0.0);

                        var svd = new SVD(A);
                        var V = svd.V();
                        var U = svd.U();
                        var w = svd.singularValues();
                        var threshold = n * Const.QL_EPSILON;

                        for (var i = 0; i < n; ++i)
                        {
                            if (w[i] > threshold)
                            {
                                double u = 0;
                                var zero = 0;
                                for (var kk = 0; kk < U.rows(); kk++)
                                {
                                    u += U[kk, i] * b[zero++] / w[i];
                                }

                                for (var jj = 0; jj < n; ++jj)
                                {
                                    xr[jj] += u * V[jj, i];
                                }
                            }
                        }

                        if (norm(xr - x) > tol)
                        {
                            QAssert.Fail("least square solution does not match (norm = "
                                         + norm(x - xr) + ")");

                        }
                    }
                }
            }
        }

        [Fact]
        public void testInverse()
        {

            // Testing LU inverse calculation
            setup();

            var tol = 1.0e-12;
            Matrix[] testMatrices = { M1, M2, I, M5 };

            for (var j = 0; j < testMatrices.Length; j++)
            {
                var A = testMatrices[j];
                var invA = Matrix.inverse(A);

                var I1 = invA * A;
                var I2 = A * invA;

                var eins = new Matrix(A.rows(), A.rows(), 0.0);
                for (var i = 0; i < A.rows(); ++i)
                {
                    eins[i, i] = 1.0;
                }

                if (norm(I1 - eins) > tol)
                {
                    QAssert.Fail("inverse(A)*A does not recover unit matrix (norm = "
                                 + norm(I1 - eins) + ")");
                }

                if (norm(I2 - eins) > tol)
                {
                    QAssert.Fail("A*inverse(A) does not recover unit matrix (norm = "
                                 + norm(I1 - eins) + ")");
                }
            }
        }
    }
}
