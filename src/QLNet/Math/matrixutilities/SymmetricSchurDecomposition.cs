﻿/*
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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Extensions;

namespace QLNet.Math.MatrixUtilities
{
    //! symmetric threshold Jacobi algorithm.
    /*! Given a real symmetric matrix S, the Schur decomposition
        finds the eigenvalues and eigenvectors of S. If D is the
        diagonal matrix formed by the eigenvalues and U the
        unitarian matrix of the eigenvectors we can write the
        Schur decomposition as
        \f[ S = U \cdot D \cdot U^T \, ,\f]
        where \f$ \cdot \f$ is the standard matrix product
        and  \f$ ^T  \f$ is the transpose operator.
        This class implements the Schur decomposition using the
        symmetric threshold Jacobi algorithm. For details on the
        different Jacobi transfomations see "Matrix computation,"
        second edition, by Golub and Van Loan,
        The Johns Hopkins University Press

        \test the correctness of the returned values is tested by
              checking their properties.
    */
    [PublicAPI]
    public class SymmetricSchurDecomposition
    {
        private Vector diagonal_;
        private Matrix eigenVectors_;

        /*! \pre s must be symmetric */
        public SymmetricSchurDecomposition(Matrix s)
        {
            diagonal_ = new Vector(s.rows());
            eigenVectors_ = new Matrix(s.rows(), s.columns(), 0.0);

            QLNet.Utils.QL_REQUIRE(s.rows() > 0 && s.columns() > 0, () => "null matrix given");
            QLNet.Utils.QL_REQUIRE(s.rows() == s.columns(), () => "input matrix must be square");

            var size = s.rows();
            for (var q = 0; q < size; q++)
            {
                diagonal_[q] = s[q, q];
                eigenVectors_[q, q] = 1.0;
            }

            var ss = new Matrix(s);

            var tmpDiag = new Vector(diagonal_);
            var tmpAccumulate = new Vector(size, 0.0);
            double threshold, epsPrec = 1e-15;
            var keeplooping = true;
            int maxIterations = 100, ite = 1;
            do
            {
                //main loop
                double sum = 0;
                for (var a = 0; a < size - 1; a++)
                {
                    for (var b = a + 1; b < size; b++)
                    {
                        sum += System.Math.Abs(ss[a, b]);
                    }
                }

                if (sum.IsEqual(0.0))
                {
                    keeplooping = false;
                }
                else
                {
                    /* To speed up computation a threshold is introduced to
                       make sure it is worthy to perform the Jacobi rotation
                    */
                    if (ite < 5)
                    {
                        threshold = 0.2 * sum / (size * size);
                    }
                    else
                    {
                        threshold = 0.0;
                    }

                    int j, k, l;
                    for (j = 0; j < size - 1; j++)
                    {
                        for (k = j + 1; k < size; k++)
                        {
                            double sine, rho, cosin, heig, tang, beta;
                            var smll = System.Math.Abs(ss[j, k]);
                            if (ite > 5 &&
                                smll < epsPrec * System.Math.Abs(diagonal_[j]) &&
                                smll < epsPrec * System.Math.Abs(diagonal_[k]))
                            {
                                ss[j, k] = 0;
                            }
                            else if (System.Math.Abs(ss[j, k]) > threshold)
                            {
                                heig = diagonal_[k] - diagonal_[j];
                                if (smll < epsPrec * System.Math.Abs(heig))
                                {
                                    tang = ss[j, k] / heig;
                                }
                                else
                                {
                                    beta = 0.5 * heig / ss[j, k];
                                    tang = 1.0 / (System.Math.Abs(beta) +
                                                  System.Math.Sqrt(1 + beta * beta));
                                    if (beta < 0)
                                    {
                                        tang = -tang;
                                    }
                                }

                                cosin = 1 / System.Math.Sqrt(1 + tang * tang);
                                sine = tang * cosin;
                                rho = sine / (1 + cosin);
                                heig = tang * ss[j, k];
                                tmpAccumulate[j] -= heig;
                                tmpAccumulate[k] += heig;
                                diagonal_[j] -= heig;
                                diagonal_[k] += heig;
                                ss[j, k] = 0.0;
                                for (l = 0; l + 1 <= j; l++)
                                {
                                    jacobiRotate_(ss, rho, sine, l, j, l, k);
                                }

                                for (l = j + 1; l <= k - 1; l++)
                                {
                                    jacobiRotate_(ss, rho, sine, j, l, l, k);
                                }

                                for (l = k + 1; l < size; l++)
                                {
                                    jacobiRotate_(ss, rho, sine, j, l, k, l);
                                }

                                for (l = 0; l < size; l++)
                                {
                                    jacobiRotate_(eigenVectors_,
                                        rho, sine, l, j, l, k);
                                }
                            }
                        }
                    }

                    for (k = 0; k < size; k++)
                    {
                        tmpDiag[k] += tmpAccumulate[k];
                        diagonal_[k] = tmpDiag[k];
                        tmpAccumulate[k] = 0.0;
                    }
                }
            } while (++ite <= maxIterations && keeplooping);

            QLNet.Utils.QL_REQUIRE(ite <= maxIterations, () => "Too many iterations (" + maxIterations + ") reached");

            // sort (eigenvalues, eigenvectors)
            List<KeyValuePair<double, Vector>> temp = new InitializedList<KeyValuePair<double, Vector>>(size);
            int row, col;
            for (col = 0; col < size; col++)
            {
                var eigenVector = new Vector(size);
                eigenVectors_.column(col).ForEach((ii, xx) => eigenVector[ii] = xx);
                temp[col] = new KeyValuePair<double, Vector>(diagonal_[col], eigenVector);
            }

            // sort descending: std::greater
            temp.Sort((x, y) => y.Key.CompareTo(x.Key));
            var maxEv = temp[0].Key;
            for (col = 0; col < size; col++)
            {
                // check for round-off errors
                diagonal_[col] = System.Math.Abs(temp[col].Key / maxEv) < 1e-16 ? 0.0 : temp[col].Key;
                var sign = 1.0;
                if (temp[col].Value[0] < 0.0)
                {
                    sign = -1.0;
                }

                for (row = 0; row < size; row++)
                {
                    eigenVectors_[row, col] = sign * temp[col].Value[row];
                }
            }
        }

        public Vector eigenvalues() => diagonal_;

        public Matrix eigenvectors() => eigenVectors_;

        private void jacobiRotate_(Matrix m, double rot, double dil, int j1, int k1, int j2, int k2)
        {
            double x1, x2;
            x1 = m[j1, k1];
            x2 = m[j2, k2];
            m[j1, k1] = x1 - dil * (x2 + x1 * rot);
            m[j2, k2] = x2 + dil * (x1 - x2 * rot);
        }
    }
}
