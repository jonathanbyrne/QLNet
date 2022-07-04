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
using System;

namespace QLNet.Math.Optimization
{
    [JetBrains.Annotations.PublicAPI] public interface IConstraint
    {
        bool test(Vector param);
        Vector upperBound(Vector parameters);
        Vector lowerBound(Vector parameters);
    }

    //! Base constraint class
    [JetBrains.Annotations.PublicAPI] public class Constraint
    {
        protected IConstraint impl_;
        public bool empty() => impl_ == null;

        public Constraint() : this(null) { }
        public Constraint(IConstraint impl)
        {
            impl_ = impl;
        }

        public double update(ref Vector p, Vector direction, double beta)
        {
            var diff = beta;
            var newParams = p + diff * direction;
            var valid = test(newParams);
            var icount = 0;
            while (!valid)
            {
                if (icount > 200)
                    Utils.QL_FAIL("can't update parameter vector");
                diff *= 0.5;
                icount++;
                newParams = p + diff * direction;
                valid = test(newParams);
            }
            p += diff * direction;
            return diff;
        }

        //! Tests if params satisfy the constraint
        public virtual bool test(Vector p) => impl_.test(p);

        //! Returns upper bound for given parameters
        public virtual Vector upperBound(Vector parameters)
        {
            var result = impl_.upperBound(parameters);
            Utils.QL_REQUIRE(parameters.size() == result.size(), () =>
                             "upper bound size (" + result.size()
                             + ") not equal to params size ("
                             + parameters.size() + ")");
            return result;
        }

        //! Returns lower bound for given parameters
        public virtual Vector lowerBound(Vector parameters)
        {
            var result = impl_.lowerBound(parameters);
            Utils.QL_REQUIRE(parameters.size() == result.size(), () =>
                             "lower bound size (" + result.size()
                             + ") not equal to params size ("
                             + parameters.size() + ")");
            return result;
        }
    }

    //! No constraint
    [JetBrains.Annotations.PublicAPI] public class NoConstraint : Constraint
    {
        private class Impl : IConstraint
        {
            public bool test(Vector v) => true;

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), double.MaxValue);

            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), double.MinValue);
        }
        public NoConstraint() : base(new Impl()) { }
    }

    //! %Constraint imposing positivity to all arguments
    [JetBrains.Annotations.PublicAPI] public class PositiveConstraint : Constraint
    {
        public PositiveConstraint()
           : base(new Impl())
        {
        }

        private class Impl : IConstraint
        {
            public bool test(Vector v)
            {
                for (var i = 0; i < v.Count; ++i)
                {
                    if (v[i] <= 0.0)
                        return false;
                }
                return true;
            }

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), double.MaxValue);

            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), 0.0);
        }
    }

    //! %Constraint imposing all arguments to be in [low,high]
    [JetBrains.Annotations.PublicAPI] public class BoundaryConstraint : Constraint
    {
        public BoundaryConstraint(double low, double high)
           : base(new Impl(low, high))
        {
        }

        private class Impl : IConstraint
        {
            private double low_;
            private double high_;

            public Impl(double low, double high)
            {
                low_ = low;
                high_ = high;
            }
            public bool test(Vector v)
            {
                for (var i = 0; i < v.Count; i++)
                {
                    if (v[i] < low_ || v[i] > high_)
                        return false;
                }
                return true;
            }

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), high_);

            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), low_);
        }
    }

    //! %Constraint enforcing both given sub-constraints
    [JetBrains.Annotations.PublicAPI] public class CompositeConstraint : Constraint
    {
        public CompositeConstraint(Constraint c1, Constraint c2) : base(new Impl(c1, c2)) { }

        private class Impl : IConstraint
        {
            private Constraint c1_, c2_;

            public Impl(Constraint c1, Constraint c2)
            {
                c1_ = c1;
                c2_ = c2;
            }

            public bool test(Vector p) => c1_.test(p) && c2_.test(p);

            public Vector upperBound(Vector parameters)
            {
                var c1ub = c1_.upperBound(parameters);
                var c2ub = c2_.upperBound(parameters);
                var rtrnArray = new Vector(c1ub.size(), 0.0);

                for (var iter = 0; iter < c1ub.size(); iter++)
                {
                    rtrnArray[iter] = System.Math.Min(c1ub[iter], c2ub[iter]);
                }

                return rtrnArray;
            }

            public Vector lowerBound(Vector parameters)
            {
                var c1lb = c1_.lowerBound(parameters);
                var c2lb = c2_.lowerBound(parameters);
                var rtrnArray = new Vector(c1lb.size(), 0.0);

                for (var iter = 0; iter < c1lb.size(); iter++)
                {
                    rtrnArray[iter] = System.Math.Max(c1lb[iter], c2lb[iter]);

                }

                return rtrnArray;
            }
        }
    }

    //! %Constraint imposing i-th argument to be in [low_i,high_i] for all i
    [JetBrains.Annotations.PublicAPI] public class NonhomogeneousBoundaryConstraint : Constraint
    {
        private class Impl : IConstraint
        {
            public Impl(Vector low, Vector high)
            {
                low_ = low;
                high_ = high;
                Utils.QL_REQUIRE(low_.Count == high_.Count, () => "Upper and lower boundaries sizes are inconsistent.");
            }

            public bool test(Vector parameters)
            {
                Utils.QL_REQUIRE(parameters.size() == low_.Count, () =>
                                 "Number of parameters and boundaries sizes are inconsistent.");

                for (var i = 0; i < parameters.size(); i++)
                {
                    if (parameters[i] < low_[i] || parameters[i] > high_[i])
                        return false;
                }
                return true;
            }

            public Vector upperBound(Vector v) => high_;

            public Vector lowerBound(Vector v) => low_;

            private Vector low_, high_;
        }

        public NonhomogeneousBoundaryConstraint(Vector low, Vector high)
           : base(new Impl(low, high))
        { }
    }

}
