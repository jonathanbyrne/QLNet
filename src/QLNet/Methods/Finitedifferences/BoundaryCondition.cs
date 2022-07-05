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

using System;
using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Methods.Finitedifferences
{
    //! Abstract boundary condition class for finite difference problems
    [PublicAPI]
    public class BoundaryCondition<Operator> where Operator : IOperator
    {
        //! \todo Generalize for n-dimensional conditions
        public enum Side
        {
            None,
            Upper,
            Lower
        }

        /*! This method modifies an array \f$ u \f$ so that it satisfies the given condition. */
        public virtual void applyAfterApplying(Vector v)
        {
            throw new NotSupportedException();
        }

        /*! This method modifies an array \f$ u \f$ so that it satisfies the given condition. */
        public virtual void applyAfterSolving(Vector v)
        {
            throw new NotSupportedException();
        }

        // interface
        /*! This method modifies an operator \f$ L \f$ before it is
            applied to an array \f$ u \f$ so that \f$ v = Lu \f$ will
            satisfy the given condition. */
        public virtual void applyBeforeApplying(IOperator o)
        {
            throw new NotSupportedException();
        }

        /*! This method modifies an operator \f$ L \f$ before the linear
            system \f$ Lu' = u \f$ is solved so that \f$ u' \f$ will
            satisfy the given condition. */
        public virtual void applyBeforeSolving(IOperator o, Vector v)
        {
            throw new NotSupportedException();
        }

        /*! This method sets the current time for time-dependent boundary conditions. */
        public virtual void setTime(double t)
        {
            throw new NotSupportedException();
        }
    }

    // Time-independent boundary conditions for tridiagonal operators

    //! Neumann boundary condition (i.e., constant derivative)
    /*! \warning The value passed must not be the value of the derivative.
                 Instead, it must be comprehensive of the grid step
                 between the first two points--i.e., it must be the
                 difference between f[0] and f[1].
        \todo generalize to time-dependent conditions.

        \ingroup findiff
    */
    // NeumanBC works on TridiagonalOperator. IOperator here is for ExerciseType compatobility with options
}
