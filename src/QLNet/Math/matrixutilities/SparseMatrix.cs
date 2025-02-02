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

namespace QLNet.Math.MatrixUtilities
{
    //general sparse matrix taken from http://www.blackbeltcoder.com/Articles/algorithms/creating-a-sparse-matrix-in-net and completed for QLNet
    [PublicAPI]
    public class SparseMatrix
    {
        // Master dictionary hold rows of column dictionary
        protected Dictionary<int, Dictionary<int, double>> _rows;

        //general dimensions
        protected int rows_, columns_;

        /// <summary>
        ///     Constructs a SparseMatrix instance.
        /// </summary>
        public SparseMatrix()
        {
            _rows = new Dictionary<int, Dictionary<int, double>>();
        }

        /// <summary>
        ///     Constructs a SparseMatrix instance.
        /// </summary>
        public SparseMatrix(int rows, int columns)
        {
            rows_ = rows;
            columns_ = columns;
            _rows = new Dictionary<int, Dictionary<int, double>>();
        }

        /// <summary>
        ///     Constructs a SparseMatrix instance.
        /// </summary>
        public SparseMatrix(SparseMatrix lhs)
        {
            rows_ = lhs.rows_;
            columns_ = lhs.columns_;
            _rows = new Dictionary<int, Dictionary<int, double>>();
            foreach (var row in lhs._rows)
            {
                if (!_rows.ContainsKey(row.Key))
                {
                    _rows.Add(row.Key, new Dictionary<int, double>());
                }

                foreach (var col in row.Value)
                {
                    if (!_rows[row.Key].ContainsKey(col.Key))
                    {
                        _rows[row.Key].Add(col.Key, col.Value);
                    }
                }
            }
        }

        /// <summary>
        ///     Gets or sets the value at the specified matrix position.
        /// </summary>
        /// <param name="row">Matrix row</param>
        /// <param name="col">Matrix column</param>
        public double this[int row, int col]
        {
            get => GetAt(row, col);
            set => SetAt(row, col, value);
        }

        public static SparseMatrix operator +(SparseMatrix m1, SparseMatrix m2)
        {
            var result = new SparseMatrix(m1);

            foreach (var row in m2._rows)
            {
                foreach (var col in row.Value)
                {
                    var val = result.GetAt(row.Key, col.Key) + col.Value;
                    result.SetAt(row.Key, col.Key, val);
                }
            }

            return result;
        }

        //operator overloads
        public static SparseMatrix operator *(double a, SparseMatrix m)
        {
            var result = new SparseMatrix(m.rows(), m.columns());

            foreach (var row in m._rows)
            {
                foreach (var col in row.Value)
                {
                    var val = a * col.Value;
                    result.SetAt(row.Key, col.Key, val);
                }
            }

            return result;
        }

        public static Vector operator *(SparseMatrix m, Vector x)
        {
            var b = new Vector(x.size());

            foreach (var row in m._rows)
            {
                var val = 0.0;
                foreach (var col in row.Value)
                {
                    val += b[col.Key] * col.Value;
                }

                b[row.Key] = val;
            }

            return b;
        }

        public static SparseMatrix operator *(SparseMatrix m1, SparseMatrix m2)
        {
            QLNet.Utils.QL_REQUIRE(m1.columns() == m2.rows() && m1.rows() == m2.columns(), () => "invalid dimensions");
            var result = new SparseMatrix(m1.rows(), m2.columns());

            foreach (var row in m1._rows)
            {
                foreach (var colRight in m2._rows[row.Key])
                {
                    var val = 0.0;
                    foreach (var col in row.Value)
                    {
                        val += m2._rows[row.Key][colRight.Key] * col.Value;
                    }

                    result.SetAt(row.Key, colRight.Key, val);
                }
            }

            return result;
        }

        public void Clear()
        {
            _rows.Clear();
        }

        public int columns() => columns_;

        /// <summary>
        ///     Gets the value at the specified matrix position.
        /// </summary>
        /// <param name="row">Matrix row</param>
        /// <param name="col">Matrix column</param>
        /// <returns>Value at the specified position</returns>
        public double GetAt(int row, int col)
        {
            Dictionary<int, double> cols;
            if (_rows.TryGetValue(row, out cols))
            {
                double value = default;
                if (cols.TryGetValue(col, out value))
                {
                    return value;
                }
            }

            return default;
        }

        /// <summary>
        ///     Returns all items in the specified column.
        ///     This method is less efficent than GetRowData().
        /// </summary>
        /// <param name="col">Matrix column</param>
        /// <returns></returns>
        public IEnumerable<double> GetColumnData(int col)
        {
            foreach (var rowdata in _rows)
            {
                double result;
                if (rowdata.Value.TryGetValue(col, out result))
                {
                    yield return result;
                }
            }
        }

        /// <summary>
        ///     Returns the number of items in the specified column.
        ///     This method is less efficent than GetRowDataCount().
        /// </summary>
        /// <param name="col">Matrix column</param>
        public int GetColumnDataCount(int col)
        {
            var result = 0;

            foreach (var cols in _rows)
            {
                if (cols.Value.ContainsKey(col))
                {
                    result++;
                }
            }

            return result;
        }

        /// <summary>
        ///     Returns all items in the specified row.
        /// </summary>
        /// <param name="row">Matrix row</param>
        public IEnumerable<double> GetRowData(int row)
        {
            Dictionary<int, double> cols;
            if (_rows.TryGetValue(row, out cols))
            {
                foreach (var pair in cols)
                {
                    yield return pair.Value;
                }
            }
        }

        /// <summary>
        ///     Returns the number of items in the specified row.
        /// </summary>
        /// <param name="row">Matrix row</param>
        public int GetRowDataCount(int row)
        {
            Dictionary<int, double> cols;
            if (_rows.TryGetValue(row, out cols))
            {
                return cols.Count;
            }

            return 0;
        }

        /// <summary>
        ///     Removes the value at the specified matrix position.
        /// </summary>
        /// <param name="row">Matrix row</param>
        /// <param name="col">Matrix column</param>
        public void RemoveAt(int row, int col)
        {
            Dictionary<int, double> cols;
            if (_rows.TryGetValue(row, out cols))
            {
                // Remove column from this row
                cols.Remove(col);
                // Remove entire row if empty
                if (cols.Count == 0)
                {
                    _rows.Remove(row);
                }
            }
        }

        public int rows() => rows_;

        /// <summary>
        ///     Sets the value at the specified matrix position.
        /// </summary>
        /// <param name="row">Matrix row</param>
        /// <param name="col">Matrix column</param>
        /// <param name="value">New value</param>
        public void SetAt(int row, int col, double value)
        {
            if (EqualityComparer<double>.Default.Equals(value, default))
            {
                // Remove any existing object if value is default(T)
                RemoveAt(row, col);
            }
            else
            {
                // Set value
                Dictionary<int, double> cols;
                if (!_rows.TryGetValue(row, out cols))
                {
                    cols = new Dictionary<int, double>();
                    _rows.Add(row, cols);
                }

                cols[col] = value;
            }
        }

        public int values()
        {
            return _rows.Sum(x => GetRowDataCount(x.Key));
        }
    }
}
