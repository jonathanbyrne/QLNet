﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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

using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    //! Constrained optimization problem
    [PublicAPI]
    public class Problem
    {
        //! Constraint
        protected Constraint constraint_;
        //! Unconstrained cost function
        protected CostFunction costFunction_;

        //! current value of the local minimum
        protected Vector currentValue_;

        //! number of evaluation of cost function and its gradient
        protected int functionEvaluation_, gradientEvaluation_;

        //! function and gradient norm values at the curentValue_ (i.e. the last step)
        protected double? functionValue_, squaredNorm_;

        //! default constructor
        //public Problem(CostFunction costFunction, Constraint constraint, Vector initialValue = Array())
        public Problem(CostFunction costFunction, Constraint constraint, Vector initialValue)
        {
            costFunction_ = costFunction;
            constraint_ = constraint;
            currentValue_ = initialValue.Clone();
            QLNet.Utils.QL_REQUIRE(!constraint.empty(), () => "empty constraint given");
        }

        public Constraint constraint() => constraint_;

        public CostFunction costFunction() => costFunction_;

        public Vector currentValue() => currentValue_;

        public int functionEvaluation() => functionEvaluation_;

        public double functionValue() => functionValue_.GetValueOrDefault();

        //! call cost function gradient computation and increment
        //  evaluation counter
        public void gradient(ref Vector grad_f, Vector x)
        {
            ++gradientEvaluation_;
            costFunction_.gradient(ref grad_f, x);
        }

        public int gradientEvaluation() => gradientEvaluation_;

        public double gradientNormValue() => squaredNorm_.GetValueOrDefault();

        /*! \warning it does not reset the current minumum to any initial value
        */
        public void reset()
        {
            functionEvaluation_ = gradientEvaluation_ = 0;
            functionValue_ = squaredNorm_ = null;
        }

        public void setCurrentValue(Vector currentValue)
        {
            currentValue_ = currentValue.Clone();
        }

        public void setFunctionValue(double functionValue)
        {
            functionValue_ = functionValue;
        }

        public void setGradientNormValue(double squaredNorm)
        {
            squaredNorm_ = squaredNorm;
        }

        //! call cost function computation and increment evaluation counter
        public double value(Vector x)
        {
            ++functionEvaluation_;
            return costFunction_.value(x);
        }

        //! call cost function computation and it gradient
        public double valueAndGradient(ref Vector grad_f, Vector x)
        {
            ++functionEvaluation_;
            ++gradientEvaluation_;
            return costFunction_.valueAndGradient(ref grad_f, x);
        }

        //! call cost values computation and increment evaluation counter
        public Vector values(Vector x)
        {
            ++functionEvaluation_;
            return costFunction_.values(x);
        }
    }
}
