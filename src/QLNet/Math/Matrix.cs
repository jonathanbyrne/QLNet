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

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;

namespace QLNet.Math
{
    //! %Matrix used in linear algebra.
    /*! This class implements the concept of Matrix as used in linear
        algebra. As such, it is <b>not</b> meant to be used as a
        container.
    */
    [PublicAPI]
    public class Matrix
    {
        // Crout's LU decomposition for matrix determinant and inverse
        // stores combined lower & upper in lum[][]
        // stores row permuations into perm[]
        // returns +1 or -1 according to even or odd number of row permutations
        // lower gets dummy 1.0s on diagonal (0.0s above)
        // upper gets lum values on diagonal (0.0s below)
        public static int decompose(Matrix m, out Matrix lum, out int[] perm)
        {
            var toggle = +1; // even (+1) or odd (-1) row permutatuions
            var n = m.rows_;

            // Make a copy of Matrix m into result Matrix lu
            lum = new Matrix(n, n);
            for (var i = 0; i < n; ++i)
            for (var j = 0; j < n; ++j)
            {
                lum[i, j] = m[i, j];
            }

            // make perm[]
            perm = new int[n];
            for (var i = 0; i < n; ++i)
            {
                perm[i] = i;
            }

            for (var j = 0; j < n - 1; ++j) // process by column. note n-1
            {
                var max = System.Math.Abs(lum[j, j]);
                var piv = j;

                for (var i = j + 1; i < n; ++i) // find pivot index
                {
                    var xij = System.Math.Abs(lum[i, j]);
                    if (xij > max)
                    {
                        max = xij;
                        piv = i;
                    }
                } // i

                if (piv != j)
                {
                    lum.swapRow(piv, j);

                    var t = perm[piv]; // swap perm elements
                    perm[piv] = perm[j];
                    perm[j] = t;

                    toggle = -toggle;
                }

                var xjj = lum[j, j];
                if (xjj.IsNotEqual(0.0))
                {
                    for (var i = j + 1; i < n; ++i)
                    {
                        var xij = lum[i, j] / xjj;
                        lum[i, j] = xij;
                        for (var k = j + 1; k < n; ++k)
                        {
                            lum[i, k] -= xij * lum[j, k];
                        }
                    }
                }
            } // j

            return toggle;
        }

        public static double[] Helper(Matrix luMatrix, double[] b) // helper
        {
            var n = luMatrix.rows_;
            var x = new double[n];
            b.CopyTo(x, 0);

            for (var i = 1; i < n; ++i)
            {
                var sum = x[i];
                for (var j = 0; j < i; ++j)
                {
                    sum -= luMatrix[i, j] * x[j];
                }

                x[i] = sum;
            }

            x[n - 1] /= luMatrix[n - 1, n - 1];
            for (var i = n - 2; i >= 0; --i)
            {
                var sum = x[i];
                for (var j = i + 1; j < n; ++j)
                {
                    sum -= luMatrix[i, j] * x[j];
                }

                x[i] = sum / luMatrix[i, i];
            }

            return x;
        } // Helper

        public static Matrix inverse(Matrix m)
        {
            QLNet.Utils.QL_REQUIRE(m.rows() == m.columns(), () => "matrix is not square");
            var n = m.rows();
            var result = new Matrix(n, n);
            for (var i = 0; i < n; ++i)
            for (var j = 0; j < n; ++j)
            {
                result[i, j] = m[i, j];
            }

            Matrix lum;
            int[] perm;
            decompose(m, out lum, out perm);

            var b = new double[n];
            for (var i = 0; i < n; ++i)
            {
                for (var j = 0; j < n; ++j)
                {
                    if (i == perm[j])
                    {
                        b[j] = 1.0;
                    }
                    else
                    {
                        b[j] = 0.0;
                    }
                }

                var x = Helper(lum, b); //
                for (var j = 0; j < n; ++j)
                {
                    result[j, i] = x[j];
                }
            }

            return result;
        }

        public static Matrix outerProduct(List<double> v1begin, List<double> v2begin)
        {
            var size1 = v1begin.Count;
            QLNet.Utils.QL_REQUIRE(size1 > 0, () => "null first vector");

            var size2 = v2begin.Count;
            QLNet.Utils.QL_REQUIRE(size2 > 0, () => "null second vector");

            var result = new Matrix(size1, size2);

            for (var i = 0; i < v1begin.Count; i++)
            for (var j = 0; j < v2begin.Count; j++)
            {
                result[i, j] = v1begin[i] * v2begin[j];
            }

            return result;
        }

        public static Matrix transpose(Matrix m)
        {
            var result = new Matrix(m.columns(), m.rows());
            for (var i = 0; i < m.rows(); i++)
            for (var j = 0; j < m.columns(); j++)
            {
                result[j, i] = m[i, j];
            }

            return result;
        }

        public void fill(double value)
        {
            for (var i = 0; i < data_.Length; i++)
            {
                data_[i] = value;
            }
        }

        public void swap(int i1, int j1, int i2, int j2)
        {
            var t = this[i2, j2];
            this[i2, j2] = this[i1, j1];
            this[i1, j1] = t;
        }

        public void swapRow(int r1, int r2)
        {
            var t = row(r1);
            for (var i = 0; i < columns_; i++)
            {
                this[r1, i] = this[r2, i];
            }

            for (var i = 0; i < columns_; i++)
            {
                this[r2, i] = t[i];
            }
        }

        public override string ToString()
        {
            var to = string.Empty;
            for (var i = 0; i < rows(); i++)
            {
                to += "| ";
                for (var j = 0; j < columns(); j++)
                {
                    to += this[i, j] + " ";
                }

                to += "|" + Environment.NewLine;
            }

            return to;
        }

        #region properties

        private int rows_, columns_;

        public int rows() => rows_;

        public int columns() => columns_;

        public bool empty() => rows_ == 0 || columns_ == 0;

        private double[] data_;

        public double this[int i, int j]
        {
            get => data_[i * columns_ + j];
            set => data_[i * columns_ + j] = value;
        }

        public double this[int i]
        {
            get => data_[i];
            set => data_[i] = value;
        }

        public Vector row(int r)
        {
            var result = new Vector(columns_);
            for (var i = 0; i < columns_; i++)
            {
                result[i] = this[r, i];
            }

            return result;
        }

        public Vector column(int c)
        {
            var result = new Vector(rows_);
            for (var i = 0; i < rows_; i++)
            {
                result[i] = this[i, c];
            }

            return result;
        }

        public Vector diagonal()
        {
            var arraySize = System.Math.Min(rows(), columns());
            var tmp = new Vector(arraySize);
            for (var i = 0; i < arraySize; i++)
            {
                tmp[i] = this[i, i];
            }

            return tmp;
        }

        public Vector GetRange(int start, int length) => new Vector(data_.Skip(start).Take(length).ToList());

        #endregion

        #region Constructors

        //! creates an empty matrix
        public Matrix()
        {
        }

        //! creates a matrix with the given dimensions
        public Matrix(int rows, int columns)
        {
            data_ = new double[rows * columns];
            rows_ = rows;
            columns_ = columns;
        }

        //! creates the matrix and fills it with <tt>value</tt>
        public Matrix(int rows, int columns, double value)
        {
            data_ = new double[rows * columns];
            for (var i = 0; i < data_.Length; i++)
            {
                data_[i] = value;
            }

            rows_ = rows;
            columns_ = columns;
        }

        public Matrix(Matrix from)
        {
            data_ = !from.empty() ? (double[])from.data_.Clone() : null;
            rows_ = from.rows_;
            columns_ = from.columns_;
        }

        #endregion

        #region Algebraic operators

        /*! \pre all matrices involved in an algebraic expression must have the same size. */
        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            return operMatrix(ref m1, ref m2, (x, y) => x + y);
        }

        public static Matrix operator -(Matrix m1, Matrix m2)
        {
            return operMatrix(ref m1, ref m2, (x, y) => x - y);
        }

        public static Matrix operator *(double value, Matrix m1)
        {
            return operValue(ref m1, value, (x, y) => x * y);
        }

        public static Matrix operator /(double value, Matrix m1)
        {
            return operValue(ref m1, value, (x, y) => x / y);
        }

        public static Matrix operator *(Matrix m1, double value)
        {
            return operValue(ref m1, value, (x, y) => x * y);
        }

        public static Matrix operator /(Matrix m1, double value)
        {
            return operValue(ref m1, value, (x, y) => x / y);
        }

        private static Matrix operMatrix(ref Matrix m1, ref Matrix m2, Func<double, double, double> func)
        {
            QLNet.Utils.QL_REQUIRE(m1.rows_ == m2.rows_ && m1.columns_ == m2.columns_, () =>
                "operation on matrices with different sizes");

            var result = new Matrix(m1.rows_, m1.columns_);
            for (var i = 0; i < m1.rows_; i++)
            for (var j = 0; j < m1.columns_; j++)
            {
                result[i, j] = func(m1[i, j], m2[i, j]);
            }

            return result;
        }

        private static Matrix operValue(ref Matrix m1, double value, Func<double, double, double> func)
        {
            var result = new Matrix(m1.rows_, m1.columns_);
            for (var i = 0; i < m1.rows_; i++)
            for (var j = 0; j < m1.columns_; j++)
            {
                result[i, j] = func(m1[i, j], value);
            }

            return result;
        }

        public static Vector operator *(Vector v, Matrix m)
        {
            QLNet.Utils.QL_REQUIRE(v.Count == m.rows(), () => "vectors and matrices with different sizes ("
                                                                       + v.Count + ", " + m.rows() + "x" + m.columns() +
                                                                       ") cannot be multiplied");
            var result = new Vector(m.columns());
            for (var i = 0; i < result.Count; i++)
            {
                result[i] = v * m.column(i);
            }

            return result;
        }

        /*! \relates Matrix */
        public static Vector operator *(Matrix m, Vector v)
        {
            QLNet.Utils.QL_REQUIRE(v.Count == m.columns(), () => "vectors and matrices with different sizes ("
                                                                          + v.Count + ", " + m.rows() + "x" + m.columns() +
                                                                          ") cannot be multiplied");
            var result = new Vector(m.rows());
            for (var i = 0; i < result.Count; i++)
            {
                result[i] = m.row(i) * v;
            }

            return result;
        }

        /*! \relates Matrix */
        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            QLNet.Utils.QL_REQUIRE(m1.columns() == m2.rows(), () => "matrices with different sizes (" +
                                                                             m1.rows() + "x" + m1.columns() + ", " +
                                                                             m2.rows() + "x" + m2.columns() + ") cannot be multiplied");
            var result = new Matrix(m1.rows(), m2.columns());
            for (var i = 0; i < result.rows(); i++)
            for (var j = 0; j < result.columns(); j++)
            {
                result[i, j] = m1.row(i) * m2.column(j);
            }

            return result;
        }

        #endregion
    }
}
