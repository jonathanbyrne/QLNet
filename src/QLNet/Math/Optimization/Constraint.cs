/*
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

namespace QLNet.Math.Optimization
{
    //! Base constraint class
    [PublicAPI]
    public class Constraint
    {
        protected IConstraint impl_;

        public Constraint() : this(null)
        {
        }

        public Constraint(IConstraint impl)
        {
            impl_ = impl;
        }

        public bool empty() => impl_ == null;

        //! Returns lower bound for given parameters
        public virtual Vector lowerBound(Vector parameters)
        {
            var result = impl_.lowerBound(parameters);
            QLNet.Utils.QL_REQUIRE(parameters.size() == result.size(), () =>
                "lower bound size (" + result.size()
                                     + ") not equal to params size ("
                                     + parameters.size() + ")");
            return result;
        }

        //! Tests if params satisfy the constraint
        public virtual bool test(Vector p) => impl_.test(p);

        public double update(ref Vector p, Vector direction, double beta)
        {
            var diff = beta;
            var newParams = p + diff * direction;
            var valid = test(newParams);
            var icount = 0;
            while (!valid)
            {
                if (icount > 200)
                {
                    QLNet.Utils.QL_FAIL("can't update parameter vector");
                }

                diff *= 0.5;
                icount++;
                newParams = p + diff * direction;
                valid = test(newParams);
            }

            p += diff * direction;
            return diff;
        }

        //! Returns upper bound for given parameters
        public virtual Vector upperBound(Vector parameters)
        {
            var result = impl_.upperBound(parameters);
            QLNet.Utils.QL_REQUIRE(parameters.size() == result.size(), () =>
                "upper bound size (" + result.size()
                                     + ") not equal to params size ("
                                     + parameters.size() + ")");
            return result;
        }
    }

    //! No constraint

    //! %Constraint imposing positivity to all arguments

    //! %Constraint imposing all arguments to be in [low,high]

    //! %Constraint enforcing both given sub-constraints

    //! %Constraint imposing i-th argument to be in [low_i,high_i] for all i
}
