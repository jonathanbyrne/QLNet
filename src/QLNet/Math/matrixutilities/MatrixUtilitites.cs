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

using QLNet.Extensions;
using QLNet.Math.Optimization;

namespace QLNet.Math.MatrixUtilities
{
    public static class MatrixUtilitites
    {
        //! algorithm used for matricial pseudo square root
        public enum SalvagingAlgorithm
        {
            None,
            Spectral,
            Hypersphere,
            LowerDiagonal,
            Higham
        }

        //cost function for hypersphere and lower-diagonal algorithm
        private class HypersphereCostFunction : CostFunction
        {
            private readonly Matrix currentRoot_;
            private readonly bool lowerDiagonal_;
            private readonly int size_;
            private readonly Matrix targetMatrix_;
            private readonly Vector targetVariance_;
            private Matrix tempMatrix_, currentMatrix_;

            public HypersphereCostFunction(Matrix targetMatrix, Vector targetVariance, bool lowerDiagonal)
            {
                size_ = targetMatrix.rows();
                lowerDiagonal_ = lowerDiagonal;
                targetMatrix_ = targetMatrix;
                targetVariance_ = targetVariance;
                currentRoot_ = new Matrix(size_, size_);
                tempMatrix_ = new Matrix(size_, size_);
                currentMatrix_ = new Matrix(size_, size_);
            }

            public override double value(Vector x)
            {
                int i, j, k;
                currentRoot_.fill(1);
                if (lowerDiagonal_)
                {
                    for (i = 0; i < size_; i++)
                    {
                        for (k = 0; k < size_; k++)
                        {
                            if (k > i)
                            {
                                currentRoot_[i, k] = 0;
                            }
                            else
                            {
                                for (j = 0; j <= k; j++)
                                {
                                    if (j == k && k != i)
                                    {
                                        currentRoot_[i, k] *= System.Math.Cos(x[i * (i - 1) / 2 + j]);
                                    }
                                    else if (j != i)
                                    {
                                        currentRoot_[i, k] *= System.Math.Sin(x[i * (i - 1) / 2 + j]);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (i = 0; i < size_; i++)
                    {
                        for (k = 0; k < size_; k++)
                        {
                            for (j = 0; j <= k; j++)
                            {
                                if (j == k && k != size_ - 1)
                                {
                                    currentRoot_[i, k] *= System.Math.Cos(x[j * size_ + i]);
                                }
                                else if (j != size_ - 1)
                                {
                                    currentRoot_[i, k] *= System.Math.Sin(x[j * size_ + i]);
                                }
                            }
                        }
                    }
                }

                double temp, error = 0;
                tempMatrix_ = Matrix.transpose(currentRoot_);
                currentMatrix_ = currentRoot_ * tempMatrix_;
                for (i = 0; i < size_; i++)
                {
                    for (j = 0; j < size_; j++)
                    {
                        temp = currentMatrix_[i, j] * targetVariance_[i] * targetVariance_[j] - targetMatrix_[i, j];
                        error += temp * temp;
                    }
                }

                return error;
            }

            public override Vector values(Vector a)
            {
                QLNet.Utils.QL_FAIL("values method not implemented");
                return null;
            }
        }

        public static void normalizePseudoRoot(Matrix matrix, Matrix pseudo)
        {
            var size = matrix.rows();
            QLNet.Utils.QL_REQUIRE(size == pseudo.rows(), () =>
                "matrix/pseudo mismatch: matrix rows are " + size + " while pseudo rows are " + pseudo.columns());
            var pseudoCols = pseudo.columns();

            // row normalization
            for (var i = 0; i < size; ++i)
            {
                var norm = 0.0;
                for (var j = 0; j < pseudoCols; ++j)
                {
                    norm += pseudo[i, j] * pseudo[i, j];
                }

                if (norm > 0.0)
                {
                    var normAdj = System.Math.Sqrt(matrix[i, i] / norm);
                    for (var j = 0; j < pseudoCols; ++j)
                    {
                        pseudo[i, j] *= normAdj;
                    }
                }
            }
        }

        //! Returns the pseudo square root of a real symmetric matrix
        /*! Given a matrix \f$ M \f$, the result \f$ S \f$ is defined
            as the matrix such that \f$ S S^T = M. \f$
            If the matrix is not positive semi definite, it can
            return an approximation of the pseudo square root
            using a (user selected) salvaging algorithm.

            For more information see: "The most general methodology to create
            a valid correlation matrix for risk management and option pricing
            purposes", by R. Rebonato and P. Jдckel.
            The Journal of Risk, 2(2), Winter 1999/2000
            http://www.rebonato.com/correlationmatrix.pdf

            Revised and extended in "Monte Carlo Methods in Finance",
            by Peter Jдckel, Chapter 6.

            \pre the given matrix must be symmetric.

            \relates Matrix

            \warning Higham algorithm only works for correlation matrices.

            \test
            - the correctness of the results is tested by reproducing
              known good data.
            - the correctness of the results is tested by checking
              returned values against numerical calculations.
        */
        public static Matrix pseudoSqrt(Matrix matrix, SalvagingAlgorithm sa)
        {
            var size = matrix.rows();

#if QL_EXTRA_SAFETY_CHECKS
         checkSymmetry(matrix);
#else
            QLNet.Utils.QL_REQUIRE(size == matrix.columns(), () =>
                "non square matrix: " + size + " rows, " + matrix.columns() + " columns");
#endif

            // spectral (a.k.a Principal Component) analysis
            var jd = new SymmetricSchurDecomposition(matrix);
            var diagonal = new Matrix(size, size, 0.0);

            // salvaging algorithm
            var result = new Matrix(size, size);
            bool negative;
            switch (sa)
            {
                case SalvagingAlgorithm.None:
                    // eigenvalues are sorted in decreasing order
                    QLNet.Utils.QL_REQUIRE(jd.eigenvalues()[size - 1] >= -1e-16, () =>
                        "negative eigenvalue(s) (" + jd.eigenvalues()[size - 1] + ")");
                    result = MatrixUtilities.CholeskyDecomposition(matrix, true);
                    break;

                case SalvagingAlgorithm.Spectral:
                    // negative eigenvalues set to zero
                    for (var i = 0; i < size; i++)
                    {
                        diagonal[i, i] = System.Math.Sqrt(System.Math.Max(jd.eigenvalues()[i], 0.0));
                    }

                    result = jd.eigenvectors() * diagonal;
                    normalizePseudoRoot(matrix, result);
                    break;

                case SalvagingAlgorithm.Hypersphere:
                    // negative eigenvalues set to zero
                    negative = false;
                    for (var i = 0; i < size; ++i)
                    {
                        diagonal[i, i] = System.Math.Sqrt(System.Math.Max(jd.eigenvalues()[i], 0.0));
                        if (jd.eigenvalues()[i] < 0.0)
                        {
                            negative = true;
                        }
                    }

                    result = jd.eigenvectors() * diagonal;
                    normalizePseudoRoot(matrix, result);

                    if (negative)
                    {
                        result = hypersphereOptimize(matrix, result, false);
                    }

                    break;

                case SalvagingAlgorithm.LowerDiagonal:
                    // negative eigenvalues set to zero
                    negative = false;
                    for (var i = 0; i < size; ++i)
                    {
                        diagonal[i, i] = System.Math.Sqrt(System.Math.Max(jd.eigenvalues()[i], 0.0));
                        if (jd.eigenvalues()[i] < 0.0)
                        {
                            negative = true;
                        }
                    }

                    result = jd.eigenvectors() * diagonal;

                    normalizePseudoRoot(matrix, result);

                    if (negative)
                    {
                        result = hypersphereOptimize(matrix, result, true);
                    }

                    break;

                case SalvagingAlgorithm.Higham:
                    var maxIterations = 40;
                    var tol = 1e-6;
                    result = highamImplementation(matrix, maxIterations, tol);
                    result = MatrixUtilities.CholeskyDecomposition(result, true);
                    break;

                default:
                    QLNet.Utils.QL_FAIL("unknown salvaging algorithm");
                    break;
            }

            return result;
        }

        public static Matrix rankReducedSqrt(Matrix matrix,
            int maxRank,
            double componentRetainedPercentage,
            SalvagingAlgorithm sa)
        {
            var size = matrix.rows();

#if QL_EXTRA_SAFETY_CHECKS
         checkSymmetry(matrix);
#else
            QLNet.Utils.QL_REQUIRE(size == matrix.columns(), () =>
                "non square matrix: " + size + " rows, " + matrix.columns() + " columns");
#endif

            QLNet.Utils.QL_REQUIRE(componentRetainedPercentage > 0.0, () => "no eigenvalues retained");
            QLNet.Utils.QL_REQUIRE(componentRetainedPercentage <= 1.0, () => "percentage to be retained > 100%");
            QLNet.Utils.QL_REQUIRE(maxRank >= 1, () => "max rank required < 1");

            // spectral (a.k.a Principal Component) analysis
            var jd = new SymmetricSchurDecomposition(matrix);
            var eigenValues = jd.eigenvalues();

            // salvaging algorithm
            switch (sa)
            {
                case SalvagingAlgorithm.None:
                    // eigenvalues are sorted in decreasing order
                    QLNet.Utils.QL_REQUIRE(eigenValues[size - 1] >= -1e-16, () =>
                        "negative eigenvalue(s) (" + eigenValues[size - 1] + ")");
                    break;
                case SalvagingAlgorithm.Spectral:
                    // negative eigenvalues set to zero
                    for (var i = 0; i < size; ++i)
                    {
                        eigenValues[i] = System.Math.Max(eigenValues[i], 0.0);
                    }

                    break;
                case SalvagingAlgorithm.Higham:
                {
                    var maxIterations = 40;
                    var tolerance = 1e-6;
                    var adjustedMatrix = highamImplementation(matrix, maxIterations, tolerance);
                    jd = new SymmetricSchurDecomposition(adjustedMatrix);
                    eigenValues = jd.eigenvalues();
                }
                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown or invalid salvaging algorithm");
                    break;
            }

            // factor reduction
            double accumulate = 0;
            eigenValues.ForEach((ii, vv) => accumulate += eigenValues[ii]);
            var enough = componentRetainedPercentage * accumulate;

            if (componentRetainedPercentage.IsEqual(1.0))
            {
                // numerical glitches might cause some factors to be discarded
                enough *= 1.1;
            }

            // retain at least one factor
            var components = eigenValues[0];
            var retainedFactors = 1;
            for (var i = 1; components < enough && i < size; ++i)
            {
                components += eigenValues[i];
                retainedFactors++;
            }

            // output is granted to have a rank<=maxRank
            retainedFactors = System.Math.Min(retainedFactors, maxRank);

            var diagonal = new Matrix(size, retainedFactors, 0.0);
            for (var i = 0; i < retainedFactors; ++i)
            {
                diagonal[i, i] = System.Math.Sqrt(eigenValues[i]);
            }

            var result = jd.eigenvectors() * diagonal;

            normalizePseudoRoot(matrix, result);
            return result;
        }

        // implementation of the Higham algorithm to find the nearest correlation matrix.
        private static Matrix highamImplementation(Matrix A, int maxIterations, double tolerance)
        {
            var size = A.rows();
            Matrix R, Y = new Matrix(A), X = new Matrix(A), deltaS = new Matrix(size, size, 0.0);

            var lastX = new Matrix(X);
            var lastY = new Matrix(Y);

            for (var i = 0; i < maxIterations; ++i)
            {
                R = Y - deltaS;
                X = projectToPositiveSemidefiniteMatrix(R);
                deltaS = X - R;
                Y = projectToUnitDiagonalMatrix(X);

                // convergence test
                if (System.Math.Max(normInf(X - lastX) / normInf(X),
                        System.Math.Max(normInf(Y - lastY) / normInf(Y),
                            normInf(Y - X) / normInf(Y)))
                    <= tolerance)
                {
                    break;
                }

                lastX = X;
                lastY = Y;
            }

            // ensure we return a symmetric matrix
            for (var i = 0; i < size; ++i)
            for (var j = 0; j < i; ++j)
            {
                Y[i, j] = Y[j, i];
            }

            return Y;
        }

        // Optimization function for hypersphere and lower-diagonal algorithm
        private static Matrix hypersphereOptimize(Matrix targetMatrix, Matrix currentRoot, bool lowerDiagonal)
        {
            int i, j, k, size = targetMatrix.rows();
            var result = new Matrix(currentRoot);
            var variance = new Vector(size);
            for (i = 0; i < size; i++)
            {
                variance[i] = System.Math.Sqrt(targetMatrix[i, i]);
            }

            if (lowerDiagonal)
            {
                var approxMatrix = result * Matrix.transpose(result);
                result = MatrixUtilities.CholeskyDecomposition(approxMatrix, true);
                for (i = 0; i < size; i++)
                {
                    for (j = 0; j < size; j++)
                    {
                        result[i, j] /= System.Math.Sqrt(approxMatrix[i, i]);
                    }
                }
            }
            else
            {
                for (i = 0; i < size; i++)
                {
                    for (j = 0; j < size; j++)
                    {
                        result[i, j] /= variance[i];
                    }
                }
            }

            var optimize = new ConjugateGradient();
            var endCriteria = new EndCriteria(100, 10, 1e-8, 1e-8, 1e-8);
            var costFunction = new HypersphereCostFunction(targetMatrix, variance, lowerDiagonal);
            var constraint = new NoConstraint();

            // hypersphere vector optimization

            if (lowerDiagonal)
            {
                var theta = new Vector(size * (size - 1) / 2);
                const double eps = 1e-16;
                for (i = 1; i < size; i++)
                {
                    for (j = 0; j < i; j++)
                    {
                        theta[i * (i - 1) / 2 + j] = result[i, j];
                        if (theta[i * (i - 1) / 2 + j] > 1 - eps)
                        {
                            theta[i * (i - 1) / 2 + j] = 1 - eps;
                        }

                        if (theta[i * (i - 1) / 2 + j] < -1 + eps)
                        {
                            theta[i * (i - 1) / 2 + j] = -1 + eps;
                        }

                        for (k = 0; k < j; k++)
                        {
                            theta[i * (i - 1) / 2 + j] /= System.Math.Sin(theta[i * (i - 1) / 2 + k]);
                            if (theta[i * (i - 1) / 2 + j] > 1 - eps)
                            {
                                theta[i * (i - 1) / 2 + j] = 1 - eps;
                            }

                            if (theta[i * (i - 1) / 2 + j] < -1 + eps)
                            {
                                theta[i * (i - 1) / 2 + j] = -1 + eps;
                            }
                        }

                        theta[i * (i - 1) / 2 + j] = System.Math.Acos(theta[i * (i - 1) / 2 + j]);
                        if (j == i - 1)
                        {
                            if (result[i, i] < 0)
                            {
                                theta[i * (i - 1) / 2 + j] = -theta[i * (i - 1) / 2 + j];
                            }
                        }
                    }
                }

                var p = new Problem(costFunction, constraint, theta);
                optimize.minimize(p, endCriteria);
                theta = p.currentValue();
                result.fill(1);
                for (i = 0; i < size; i++)
                {
                    for (k = 0; k < size; k++)
                    {
                        if (k > i)
                        {
                            result[i, k] = 0;
                        }
                        else
                        {
                            for (j = 0; j <= k; j++)
                            {
                                if (j == k && k != i)
                                {
                                    result[i, k] *= System.Math.Cos(theta[i * (i - 1) / 2 + j]);
                                }
                                else if (j != i)
                                {
                                    result[i, k] *= System.Math.Sin(theta[i * (i - 1) / 2 + j]);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var theta = new Vector(size * (size - 1));
                const double eps = 1e-16;
                for (i = 0; i < size; i++)
                {
                    for (j = 0; j < size - 1; j++)
                    {
                        theta[j * size + i] = result[i, j];
                        if (theta[j * size + i] > 1 - eps)
                        {
                            theta[j * size + i] = 1 - eps;
                        }

                        if (theta[j * size + i] < -1 + eps)
                        {
                            theta[j * size + i] = -1 + eps;
                        }

                        for (k = 0; k < j; k++)
                        {
                            theta[j * size + i] /= System.Math.Sin(theta[k * size + i]);
                            if (theta[j * size + i] > 1 - eps)
                            {
                                theta[j * size + i] = 1 - eps;
                            }

                            if (theta[j * size + i] < -1 + eps)
                            {
                                theta[j * size + i] = -1 + eps;
                            }
                        }

                        theta[j * size + i] = System.Math.Acos(theta[j * size + i]);
                        if (j == size - 2)
                        {
                            if (result[i, j + 1] < 0)
                            {
                                theta[j * size + i] = -theta[j * size + i];
                            }
                        }
                    }
                }

                var p = new Problem(costFunction, constraint, theta);
                optimize.minimize(p, endCriteria);
                theta = p.currentValue();
                result.fill(1);
                for (i = 0; i < size; i++)
                {
                    for (k = 0; k < size; k++)
                    {
                        for (j = 0; j <= k; j++)
                        {
                            if (j == k && k != size - 1)
                            {
                                result[i, k] *= System.Math.Cos(theta[j * size + i]);
                            }
                            else if (j != size - 1)
                            {
                                result[i, k] *= System.Math.Sin(theta[j * size + i]);
                            }
                        }
                    }
                }
            }

            for (i = 0; i < size; i++)
            {
                for (j = 0; j < size; j++)
                {
                    result[i, j] *= variance[i];
                }
            }

            return result;
        }

        // Matrix infinity norm. See Golub and van Loan (2.3.10) or
        // <http://en.wikipedia.org/wiki/Matrix_norm>
        private static double normInf(Matrix M)
        {
            var rows = M.rows();
            var cols = M.columns();
            var norm = 0.0;
            for (var i = 0; i < rows; ++i)
            {
                var colSum = 0.0;
                for (var j = 0; j < cols; ++j)
                {
                    colSum += System.Math.Abs(M[i, j]);
                }

                norm = System.Math.Max(norm, colSum);
            }

            return norm;
        }

        // Take a matrix and make all the eigenvalues non-negative
        private static Matrix projectToPositiveSemidefiniteMatrix(Matrix M)
        {
            var size = M.rows();
            QLNet.Utils.QL_REQUIRE(size == M.columns(), () => "matrix not square");

            var diagonal = new Matrix(size, size);
            var jd = new SymmetricSchurDecomposition(M);
            for (var i = 0; i < size; ++i)
            {
                diagonal[i, i] = System.Math.Max(jd.eigenvalues()[i], 0.0);
            }

            var result = jd.eigenvectors() * diagonal * Matrix.transpose(jd.eigenvectors());
            return result;
        }

        // Take a matrix and make all the diagonal entries 1.
        private static Matrix projectToUnitDiagonalMatrix(Matrix M)
        {
            var size = M.rows();
            QLNet.Utils.QL_REQUIRE(size == M.columns(), () => "matrix not square");

            var result = new Matrix(M);
            for (var i = 0; i < size; ++i)
            {
                result[i, i] = 1.0;
            }

            return result;
        }
    }
}
