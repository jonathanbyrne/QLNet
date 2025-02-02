﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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
using System.Linq;

namespace QLNet.Math.Optimization
{
    //!  Cost function abstract class for optimization problem
    public abstract class CostFunction
    {
        //! method to overload to compute the cost function values in x
        public abstract Vector values(Vector x);

        //! Default epsilon for finite difference method :
        public virtual double finiteDifferenceEpsilon() => 1e-8;

        //! method to overload to compute grad_f, the first derivative of
        //  the cost function with respect to x
        public virtual void gradient(ref Vector grad, Vector x)
        {
            double eps = finiteDifferenceEpsilon(), fp, fm;
            var xx = new Vector(x);
            for (var i = 0; i < x.Count; i++)
            {
                xx[i] += eps;
                fp = value(xx);
                xx[i] -= 2.0 * eps;
                fm = value(xx);
                grad[i] = 0.5 * (fp - fm) / eps;
                xx[i] = x[i];
            }
        }

        //! method to overload to compute J_f, the jacobian of
        // the cost function with respect to x
        public virtual void jacobian(Matrix jac, Vector x)
        {
            var eps = finiteDifferenceEpsilon();
            var xx = new Vector(x);
            var fp = new Vector();
            var fm = new Vector();
            for (var i = 0; i < x.size(); ++i)
            {
                xx[i] += eps;
                fp = values(xx);
                xx[i] -= 2.0 * eps;
                fm = values(xx);
                for (var j = 0; j < fp.size(); ++j)
                {
                    jac[j, i] = 0.5 * (fp[j] - fm[j]) / eps;
                }

                xx[i] = x[i];
            }
        }

        //! method to overload to compute the cost function value in x
        public virtual double value(Vector x)
        {
            var v = Vector.Sqrt(x);
            return System.Math.Sqrt(v.Sum(a => a) / Convert.ToDouble(v.size()));
        }

        //! method to overload to compute grad_f, the first derivative of
        //  the cost function with respect to x and also the cost function
        public virtual double valueAndGradient(ref Vector grad, Vector x)
        {
            gradient(ref grad, x);
            return value(x);
        }

        //! method to overload to compute J_f, the jacobian of
        // the cost function with respect to x and also the cost function
        public virtual Vector valuesAndJacobian(Matrix jac, Vector x)
        {
            jacobian(jac, x);
            return values(x);
        }
    }
}
