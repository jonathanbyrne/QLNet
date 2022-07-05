/*
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

namespace QLNet.Math.Optimization
{
    //! Base class for least square problem
    public abstract class LeastSquareProblem
    {
        //! size of the problem ie size of target vector
        public abstract int size();

        //! compute the target vector and the values of the function to fit
        public abstract void targetAndValue(Vector x, ref Vector target, ref Vector fct2fit);

        //        ! compute the target vector, the values of the function to fit
        //            and the matrix of derivatives
        //
        public abstract void targetValueAndGradient(Vector x, ref Matrix grad_fct2fit, ref Vector target, ref Vector fct2fit);
    }

    //! Cost function for least-square problems
    //    ! Implements a cost function using the interface provided by
    //        the LeastSquareProblem class.
    //

    //! Non-linear least-square method.
    //    ! Using a given optimization algorithm (default is conjugate
    //        gradient),
    //
    //        \f[ min \{ r(x) : x in R^n \} \f]
    //
    //        where \f$ r(x) = |f(x)|^2 \f$ is the Euclidean norm of \f$
    //        f(x) \f$ for some vector-valued function \f$ f \f$ from
    //        \f$ R^n \f$ to \f$ R^m \f$,
    //        \f[ f = (f_1, ..., f_m) \f]
    //        with \f$ f_i(x) = b_i - \phi(x,t_i) \f$ where \f$ b \f$ is the
    //        vector of target data and \f$ phi \f$ is a scalar function.
    //
    //        Assuming the differentiability of \f$ f \f$, the gradient of
    //        \f$ r \f$ is defined by
    //        \f[ grad r(x) = f'(x)^t.f(x) \f]
    //
}
