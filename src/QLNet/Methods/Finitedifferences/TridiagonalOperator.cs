﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Andrea Maggiulli

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
using QLNet.Extensions;
using QLNet.Math;

namespace QLNet.Methods.Finitedifferences
{
    //! Base implementation for tridiagonal operator
    /*! \warning to use real time-dependant algebra, you must overload
                 the corresponding operators in the inheriting
                 time-dependent class.

        \ingroup findiff
    */
    [PublicAPI]
    public class TridiagonalOperator : IOperator
    {
        //! encapsulation of time-setting logic
        public abstract class TimeSetter
        {
            public abstract void setTime(double t, IOperator L);
        }

        protected Vector diagonal_, lowerDiagonal_, upperDiagonal_;
        protected TimeSetter timeSetter_;

        public TridiagonalOperator() : this(0)
        {
        }

        public TridiagonalOperator(int size)
        {
            if (size >= 2)
            {
                diagonal_ = new Vector(size);
                lowerDiagonal_ = new Vector(size - 1);
                upperDiagonal_ = new Vector(size - 1);
            }
            else if (size == 0)
            {
                diagonal_ = new Vector(0);
                lowerDiagonal_ = new Vector(0);
                upperDiagonal_ = new Vector(0);
            }
            else
            {
                throw new ArgumentException("invalid size (" + size + ") for tridiagonal operator " +
                                            "(must be null or >= 2)");
            }
        }

        public TridiagonalOperator(Vector low, Vector mid, Vector high)
        {
            diagonal_ = mid.Clone();
            lowerDiagonal_ = low.Clone();
            upperDiagonal_ = high.Clone();

            QLNet.Utils.QL_REQUIRE(low.Count == mid.Count - 1, () => "wrong size for lower diagonal vector");
            QLNet.Utils.QL_REQUIRE(high.Count == mid.Count - 1, () => "wrong size for upper diagonal vector");
        }

        public IOperator add
            (IOperator A, IOperator B)
        {
            var D1 = A as TridiagonalOperator;
            var D2 = B as TridiagonalOperator;

            Vector low = D1.lowerDiagonal_ + D2.lowerDiagonal_,
                mid = D1.diagonal_ + D2.diagonal_,
                high = D1.upperDiagonal_ + D2.upperDiagonal_;
            var result = new TridiagonalOperator(low, mid, high);
            return result;
        }

        //! apply operator to a given array
        public Vector applyTo(Vector v)
        {
            QLNet.Utils.QL_REQUIRE(v.Count == size(), () => "vector of the wrong size (" + v.Count + "instead of " + size() + ")");

            var result = new Vector(size());

            // transform(InputIterator1 start1, InputIterator1 finish1, InputIterator2 start2, OutputIterator result,
            // BinaryOperation binary_op)
            result = Vector.DirectMultiply(diagonal_, v);

            // matricial product
            result[0] += upperDiagonal_[0] * v[1];
            for (var j = 1; j <= size() - 2; j++)
            {
                result[j] += lowerDiagonal_[j - 1] * v[j - 1] + upperDiagonal_[j] * v[j + 1];
            }

            result[size() - 1] += lowerDiagonal_[size() - 2] * v[size() - 2];

            return result;
        }

        public object Clone() => MemberwiseClone();

        public Vector diagonal() => diagonal_;

        //! identity instance
        public IOperator identity(int size)
        {
            var I = new TridiagonalOperator(new Vector(size - 1, 0.0), // lower diagonal
                new Vector(size, 1.0), // diagonal
                new Vector(size - 1, 0.0)); // upper diagonal
            return I;
        }

        public bool isTimeDependent() => timeSetter_ != null;

        public Vector lowerDiagonal() => lowerDiagonal_;

        public IOperator multiply(double a, IOperator o)
        {
            var D = o as TridiagonalOperator;
            Vector low = D.lowerDiagonal_ * a,
                mid = D.diagonal_ * a,
                high = D.upperDiagonal_ * a;
            var result = new TridiagonalOperator(low, mid, high);
            return result;
        }

        public void setFirstRow(double valB, double valC)
        {
            diagonal_[0] = valB;
            upperDiagonal_[0] = valC;
        }

        public void setLastRow(double valA, double valB)
        {
            lowerDiagonal_[size() - 2] = valA;
            diagonal_[size() - 1] = valB;
        }

        public void setMidRow(int i, double valA, double valB, double valC)
        {
            QLNet.Utils.QL_REQUIRE(i >= 1 && i <= size() - 2, () => "out of range in TridiagonalSystem::setMidRow");
            lowerDiagonal_[i - 1] = valA;
            diagonal_[i] = valB;
            upperDiagonal_[i] = valC;
        }

        public void setMidRows(double valA, double valB, double valC)
        {
            for (var i = 1; i <= size() - 2; i++)
            {
                lowerDiagonal_[i - 1] = valA;
                diagonal_[i] = valB;
                upperDiagonal_[i] = valC;
            }
        }

        public void setTime(double t)
        {
            if (timeSetter_ != null)
            {
                timeSetter_.setTime(t, this);
            }
        }

        public int size() => diagonal_.Count;

        //! solve linear system for a given right-hand side
        public Vector solveFor(Vector rhs)
        {
            QLNet.Utils.QL_REQUIRE(rhs.Count == size(), () => "rhs has the wrong size");

            Vector result = new Vector(size()), tmp = new Vector(size());

            var bet = diagonal_[0];
            QLNet.Utils.QL_REQUIRE(bet.IsNotEqual(0.0), () => "division by zero");
            result[0] = rhs[0] / bet;

            for (var j = 1; j < size(); j++)
            {
                tmp[j] = upperDiagonal_[j - 1] / bet;
                bet = diagonal_[j] - lowerDiagonal_[j - 1] * tmp[j];
                QLNet.Utils.QL_REQUIRE(bet.IsNotEqual(0.0), () => "division by zero");
                result[j] = (rhs[j] - lowerDiagonal_[j - 1] * result[j - 1]) / bet;
            }

            // cannot be j>=0 with Size j
            for (var j = size() - 2; j > 0; --j)
            {
                result[j] -= tmp[j + 1] * result[j + 1];
            }

            result[0] -= tmp[1] * result[1];
            return result;
        }

        //! solve linear system with SOR approach
        public Vector SOR(Vector rhs, double tol)
        {
            QLNet.Utils.QL_REQUIRE(rhs.Count == size(), () => "rhs has the wrong size");

            // initial guess
            var result = rhs.Clone();

            // solve tridiagonal system with SOR technique
            var omega = 1.5;
            var err = 2.0 * tol;
            double temp;
            int i;
            for (var sorIteration = 0; err > tol; sorIteration++)
            {
                QLNet.Utils.QL_REQUIRE(sorIteration < 100000, () =>
                    "tolerance (" + tol + ") not reached in " + sorIteration + " iterations. " + "The error still is " + err);

                temp = omega * (rhs[0] -
                                upperDiagonal_[0] * result[1] -
                                diagonal_[0] * result[0]) / diagonal_[0];
                err = temp * temp;
                result[0] += temp;

                for (i = 1; i < size() - 1; i++)
                {
                    temp = omega * (rhs[i] -
                                    upperDiagonal_[i] * result[i + 1] -
                                    diagonal_[i] * result[i] -
                                    lowerDiagonal_[i - 1] * result[i - 1]) / diagonal_[i];
                    err += temp * temp;
                    result[i] += temp;
                }

                temp = omega * (rhs[i] -
                                diagonal_[i] * result[i] -
                                lowerDiagonal_[i - 1] * result[i - 1]) / diagonal_[i];
                err += temp * temp;
                result[i] += temp;
            }

            return result;
        }

        public IOperator subtract(IOperator A, IOperator B)
        {
            var D1 = A as TridiagonalOperator;
            var D2 = B as TridiagonalOperator;

            Vector low = D1.lowerDiagonal_ - D2.lowerDiagonal_,
                mid = D1.diagonal_ - D2.diagonal_,
                high = D1.upperDiagonal_ - D2.upperDiagonal_;
            var result = new TridiagonalOperator(low, mid, high);
            return result;
        }

        public Vector upperDiagonal() => upperDiagonal_;
    }
}
