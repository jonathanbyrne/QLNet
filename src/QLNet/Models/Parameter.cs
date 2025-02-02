﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Models
{
    //! Base class for model arguments
    [PublicAPI]
    public class Parameter
    {
        //! Base class for model parameter implementation
        public abstract class Impl
        {
            public abstract double value(Vector p, double t);
        }

        protected Constraint constraint_;
        protected Impl impl_;
        protected Vector params_;

        public Parameter()
        {
            constraint_ = new NoConstraint();
        }

        protected Parameter(int size, Impl impl, Constraint constraint)
        {
            impl_ = impl;
            params_ = new Vector(size);
            constraint_ = constraint;
        }

        public Constraint constraint() => constraint_;

        public Impl implementation() => impl_;

        public Vector parameters() => params_;

        public void setParam(int i, double x)
        {
            params_[i] = x;
        }

        public int size() => params_.size();

        public bool testParams(Vector p) => constraint_.test(p);

        public double value(double t) => impl_.value(params_, t);
    }

    //! Standard constant parameter \f$ a(t) = a \f$

    //! %Parameter which is always zero \f$ a(t) = 0 \f$

    //! Piecewise-constant parameter
    //    ! \f$ a(t) = a_i if t_{i-1} \geq t < t_i \f$.
    //        This kind of parameter is usually used to enhance the fitting of a
    //        model
    //

    //! Deterministic time-dependent parameter used for yield-curve fitting
}
