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
using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    //! Levenberg-Marquardt optimization method
    /*! This implementation is based on MINPACK
        (<http://www.netlib.org/minpack>,
        <http://www.netlib.org/cephes/linalg.tgz>)
        It has a built in fd scheme to compute
        the jacobian, which is used by default.
        If useCostFunctionsJacobian is true the
        corresponding method in the cost function
        of the problem is used instead. Note that
        the default implementation of the jacobian
        in CostFunction uses a central difference
        (oder 2, but requiring more function
        evaluations) compared to the forward
        difference implemented here (order 1).
    */
    [PublicAPI]
    public class LevenbergMarquardt : OptimizationMethod
    {
        private Problem currentProblem_;
        private double epsfcn_, xtol_, gtol_;
        private int info_;
        private Vector initCostValues_;
        private Matrix initJacobian_;
        private bool useCostFunctionsJacobian_;

        public LevenbergMarquardt() : this(1.0e-8, 1.0e-8, 1.0e-8)
        {
        }

        public LevenbergMarquardt(double epsfcn, double xtol, double gtol, bool useCostFunctionsJacobian = false)
        {
            info_ = 0;
            epsfcn_ = epsfcn;
            xtol_ = xtol;
            gtol_ = gtol;
            useCostFunctionsJacobian_ = useCostFunctionsJacobian;
        }

        public Vector fcn(int m, int n, Vector x, int iflag)
        {
            var xt = new Vector(x);
            Vector fvec;
            // constraint handling needs some improvement in the future:
            // starting point should not be close to a constraint violation
            if (currentProblem_.constraint().test(xt))
            {
                fvec = new Vector(currentProblem_.values(xt));
            }
            else
            {
                fvec = new Vector(initCostValues_);
            }

            return fvec;
        }

        public int getInfo() => info_;

        public Matrix jacFcn(int m, int n, Vector x, int iflag)
        {
            var xt = new Vector(x);
            Matrix fjac;
            // constraint handling needs some improvement in the future:
            // starting point should not be close to a constraint violation
            if (currentProblem_.constraint().test(xt))
            {
                var tmp = new Matrix(m, n);
                currentProblem_.costFunction().jacobian(tmp, xt);
                var tmpT = Matrix.transpose(tmp);
                fjac = new Matrix(tmpT);
            }
            else
            {
                var tmpT = Matrix.transpose(initJacobian_);
                fjac = new Matrix(tmpT);
            }

            return fjac;
        }

        public override EndCriteria.Type minimize(Problem P, EndCriteria endCriteria)
        {
            var ecType = EndCriteria.Type.None;
            P.reset();
            var x_ = P.currentValue();
            currentProblem_ = P;
            initCostValues_ = P.costFunction().values(x_);
            var m = initCostValues_.size();
            var n = x_.size();
            if (useCostFunctionsJacobian_)
            {
                initJacobian_ = new Matrix(m, n);
                P.costFunction().jacobian(initJacobian_, x_);
            }

            var xx = new Vector(x_);
            Vector fvec = new Vector(m), diag = new Vector(n);

            var mode = 1;
            double factor = 1;
            var nprint = 0;
            var info = 0;
            var nfev = 0;

            var fjac = new Matrix(m, n);

            var ldfjac = m;

            List<int> ipvt = new InitializedList<int>(n);
            Vector qtf = new Vector(n), wa1 = new Vector(n), wa2 = new Vector(n), wa3 = new Vector(n), wa4 = new Vector(m);

            // call lmdif to minimize the sum of the squares of m functions
            // in n variables by the Levenberg-Marquardt algorithm.
            Func<int, int, Vector, int, Matrix> j = null;
            if (useCostFunctionsJacobian_)
            {
                j = jacFcn;
            }

            // requirements; check here to get more detailed error messages.
            QLNet.Utils.QL_REQUIRE(n > 0, () => "no variables given");
            QLNet.Utils.QL_REQUIRE(m >= n, () => $"less functions ({m}) than available variables ({n})");
            QLNet.Utils.QL_REQUIRE(endCriteria.functionEpsilon() >= 0.0, () => "negative f tolerance");
            QLNet.Utils.QL_REQUIRE(xtol_ >= 0.0, () => "negative x tolerance");
            QLNet.Utils.QL_REQUIRE(gtol_ >= 0.0, () => "negative g tolerance");
            QLNet.Utils.QL_REQUIRE(endCriteria.maxIterations() > 0, () => "null number of evaluations");

            MINPACK.lmdif(m, n, xx, ref fvec,
                endCriteria.functionEpsilon(),
                xtol_,
                gtol_,
                endCriteria.maxIterations(),
                epsfcn_,
                diag, mode, factor,
                nprint, ref info, ref nfev, ref fjac,
                ldfjac, ref ipvt, ref qtf,
                wa1, wa2, wa3, wa4,
                fcn, j);
            info_ = info;
            // check requirements & endCriteria evaluation
            QLNet.Utils.QL_REQUIRE(info != 0, () => "MINPACK: improper input parameters");
            if (info != 6)
            {
                ecType = EndCriteria.Type.StationaryFunctionValue;
            }

            endCriteria.checkMaxIterations(nfev, ref ecType);
            QLNet.Utils.QL_REQUIRE(info != 7, () => "MINPACK: xtol is too small. no further " +
                                                             "improvement in the approximate " +
                                                             "solution x is possible.");
            QLNet.Utils.QL_REQUIRE(info != 8, () => "MINPACK: gtol is too small. fvec is " +
                                                             "orthogonal to the columns of the " +
                                                             "jacobian to machine precision.");
            // set problem
            x_ = new Vector(xx.GetRange(0, n));
            P.setCurrentValue(x_);
            P.setFunctionValue(P.costFunction().value(x_));

            return ecType;
        }
    }
}
